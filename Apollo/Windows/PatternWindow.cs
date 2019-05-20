﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FontWeight = Avalonia.Media.FontWeight;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Avalonia.Threading;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;

namespace Apollo.Windows {
    public class PatternWindow: Window, ISelectParentViewer {
        public int? IExpanded {
            get => _pattern.Expanded;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Pattern _pattern;
        Track _track;
        
        Launchpad _launchpad;
        private Launchpad Launchpad {
            get => _launchpad;
            set {
                if (_launchpad != null) {
                    _launchpad.PatternWindow = null;
                    _launchpad.Clear();
                }

                _launchpad = value;

                if (_launchpad != null) {
                    _launchpad.PatternWindow?.Close();
                    _launchpad.PatternWindow = this;
                    _launchpad.Clear();
                    PlayExit = _launchpad.Send;
                } else PlayExit = null;

                origin = gesturePoint = -1;

                if (historyShowing) RenderHistory();
                else if (IsArrangeValid)
                    for (int i = 0; i < _pattern[_pattern.Expanded].Screen.Length; i++)
                        _launchpad?.Send(new Signal(Launchpad, (byte)i, _pattern[_pattern.Expanded].Screen[i]));
            }
        }

        TextBlock TitleText;
        UndoButton UndoButton;
        RedoButton RedoButton;
        ComboBox PortSelector, PlaybackMode;
        LaunchpadGrid Editor;
        Controls Contents;
        ContextMenu FrameContextMenu;
        ColorPicker ColorPicker;
        ColorHistory ColorHistory;
        Dial Duration, Gate, Choke;
        Button Import, Play, Fire;

        int origin = -1;
        int gesturePoint = -1;
        bool gestureUsed = false;
        bool historyShowing = false;

        bool _locked = false;
        bool Locked {
            get => _locked;
            set {
                _locked = value;
                PortSelector.IsEnabled = UndoButton.IsEnabled = RedoButton.IsEnabled = Duration.Enabled = Gate.Enabled = Import.IsEnabled = Play.IsEnabled = Fire.IsEnabled = ColorPicker.IsEnabled = ColorHistory.IsEnabled = !_locked;
            }
        }

        Action<Signal> PlayExit;
        
        private void UpdateTitle() => UpdateTitle(_track.ParentIndex.Value, _track.Name);
        private void UpdateTitle(int index) => UpdateTitle(index, _track.Name);
        private void UpdateTitle(string name) => UpdateTitle(_track.ParentIndex.Value, name);
        private void UpdateTitle(int index, string name)
            => Title = TitleText.Text = $"Editing Pattern - {name.Replace("#", (index + 1).ToString())}";

        private void UpdateTopmost(bool value) => Topmost = value;

        private void UpdatePorts() {
            List<Launchpad> ports = (from i in MIDI.Devices where i.Available && i.Type != Launchpad.LaunchpadType.Unknown select i).ToList();
            if (Launchpad != null && (!Launchpad.Available || Launchpad.Type == Launchpad.LaunchpadType.Unknown)) ports.Add(Launchpad);

            PortSelector.Items = ports;
            PortSelector.SelectedIndex = -1;
            PortSelector.SelectedItem = Launchpad;
        }

        private void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);
        
        private void SetAlwaysShowing() {
            for (int i = 1; i < Contents.Count; i++)
                ((FrameDisplay)Contents[i]).FrameAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((FrameDisplay)Contents.Last()).FrameAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Frame frame, bool ignoreExpanded = false) {
            if (Contents.Count > 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 1;

            FrameDisplay viewer = new FrameDisplay(frame, _pattern);
            viewer.FrameAdded += Frame_Insert;
            viewer.FrameRemoved += Frame_Remove;
            viewer.FrameSelected += Frame_Select;
            frame.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();

            if (!ignoreExpanded && index <= _pattern.Expanded) _pattern.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (index < _pattern.Expanded) _pattern.Expanded--;
            else if (index == _pattern.Expanded) Frame_Select(Math.Max(0, _pattern.Expanded - 1));

            _pattern[index].Info = null;
            Contents.RemoveAt(index + 1);

            SetAlwaysShowing();

            if (Contents.Count == 2) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;
        }

        public SelectionManager Selection = new SelectionManager();

        public PatternWindow(Pattern pattern) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _pattern = pattern;
            _track = Track.Get(_pattern);

            TitleText = this.Get<TextBlock>("Title");
            UndoButton = this.Get<UndoButton>("UndoButton");
            RedoButton = this.Get<RedoButton>("RedoButton");

            Editor = this.Get<LaunchpadGrid>("Editor");

            Duration = this.Get<Dial>("Duration");
            
            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_pattern.Gate * 100;

            PlaybackMode = this.Get<ComboBox>("PlaybackMode");
            PlaybackMode.SelectedItem = _pattern.Mode;

            Choke = this.Get<Dial>("Choke");
            if (_pattern.Choke != null) {
                Choke.Enabled = true;
                Choke.RawValue = _pattern.Choke.Value;
            }

            Import = this.Get<Button>("Import");
            Play = this.Get<Button>("Play");
            Fire = this.Get<Button>("Fire");

            FrameContextMenu = (ContextMenu)this.Resources["FrameContextMenu"];
            FrameContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(FrameContextMenu_Click));

            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DropEvent, Drop);

            Contents = this.Get<StackPanel>("Frames").Children;

            for (int i = 0; i < _pattern.Count; i++)
                Contents_Insert(i, _pattern[i], true);
            
            if (_pattern.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;

            Launchpad = _track.Launchpad;

            PortSelector = this.Get<ComboBox>("PortSelector");
            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            ColorPicker = this.Get<ColorPicker>("ColorPicker");
            ColorHistory = this.Get<ColorHistory>("ColorHistory");

            ColorPicker.SetColor(ColorHistory.GetColor(0)?? new Color());
            ColorHistory.Select(ColorPicker.Color.Clone(), true);
            
            Frame_Select(_pattern.Expanded);
            Selection.Select(_pattern[_pattern.Expanded]);
        }

        public void Expand(int? index) {
            if (index != null) Frame_Select(index.Value);
        }

        private void Loaded(object sender, EventArgs e) {
            _track.ParentIndexChanged += UpdateTitle;
            _track.NameChanged += UpdateTitle;
            UpdateTitle();

            ColorHistory.HistoryChanged += RenderHistory;
        }

        private void Unloaded(object sender, EventArgs e) {
            Locked = false;

            _pattern.Window = null;

            if (Launchpad != null) {
                Launchpad.PatternWindow = null;
                Launchpad.Clear();
            }
            
            _track.ParentIndexChanged -= UpdateTitle;
            _track.NameChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            ColorHistory.HistoryChanged -= RenderHistory;

            Program.WindowClose(this);
        }

        private void Port_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)PortSelector.SelectedItem;

            if (selected != null && Launchpad != selected) {
                Launchpad = selected;
                UpdatePorts();
            }
        }

        private void Frame_Insert(int index) {
            Frame reference = _pattern[Math.Max(0, index - 1)];

            Frame_Insert(index, new Frame(
                reference.Mode,
                reference.Length.Clone(),
                reference.Time
            ));
        }

        private void Frame_Insert(int index, Frame frame) {
            if (Locked) return;

            Frame reference = _pattern[Math.Max(0, index - 1)];

            if (Preferences.CopyPreviousFrame)
                for (int i = 0; i < reference.Screen.Length; i++)
                    frame.Screen[i] = reference.Screen[i].Clone();
            
            Frame r = frame.Clone();
            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Frame Added", () => {
                ((Pattern)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Pattern)Track.TraversePath(path)).Insert(index, r.Clone());
            });

            _pattern.Insert(index, frame);
        }

        private void Frame_InsertStart() => Frame_Insert(0);

        private void Frame_Remove(int index) {
            if (Locked) return;

            if (_pattern.Count == 1) return;

            Frame u = _pattern[index].Clone();
            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Frame Deleted", () => {
                ((Pattern)Track.TraversePath(path)).Insert(index, u.Clone());
            }, () => {
                ((Pattern)Track.TraversePath(path)).Remove(index);
            });

            _pattern.Remove(index);
        }

        public void Frame_Select(int index) {
            if (Locked) return;

            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.FontWeight = FontWeight.Normal;

            _pattern.Expanded = index;

            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.FontWeight = FontWeight.Bold;
            
            Duration.UsingSteps = _pattern[_pattern.Expanded].Mode;
            Duration.Length = _pattern[_pattern.Expanded].Length;
            Duration.RawValue = _pattern[_pattern.Expanded].Time;

            Editor.RenderFrame(_pattern[_pattern.Expanded]);

            for (int i = 0; i < _pattern[_pattern.Expanded].Screen.Length; i++)
                Launchpad?.Send(new Signal(Launchpad, (byte)i, _pattern[_pattern.Expanded].Screen[i]));
        }

        private void Frame_Action(string action) => Frame_Action(action, false);
        private void Frame_Action(string action, bool right) => Selection.Action(action, _pattern, (right? _pattern.Count : 0) - 1);

        private void FrameContextMenu_Click(object sender, EventArgs e) {
            this.Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Frame_Action((string)((MenuItem)item).Header, true);
        }
        
        private void Frame_AfterClick(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right)
                FrameContextMenu.Open((Control)sender);

            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (Locked) return;

            if (e.Key == Key.Enter) {
                if (e.Modifiers == InputModifiers.Shift) HandleGesture(1, -1);
                else HandleGesture(-1, 1);
            
            } else if (e.Key == Key.Insert) HandleGesture(1, 1);
            else if (e.Key == Key.Delete) HandleGesture(-1, -1);

            if (Program.Project.Undo.HandleKey(e) || Selection.Start == null || Selection.ActionKey(e)) {
                this.Focus();
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Left) {
                Selection.Move(false, e.Modifiers == InputModifiers.Shift);
                Frame_Select(Selection.Start.IParentIndex.Value);

            } else if (e.Key == Key.Down || e.Key == Key.Right) {
                Selection.Move(true, e.Modifiers == InputModifiers.Shift);
                Frame_Select(Selection.Start.IParentIndex.Value);
            }
        }

        private void ColorPicker_Changed(Color color, Color old) => ColorHistory.Select(color.Clone());

        private void ColorHistory_Changed(Color color) {
            ColorPicker.SetColor(color.Clone());
            RenderHistory();
        }

        private void RenderHistory() {
            if (!historyShowing) return;
            ColorHistory.Render(Launchpad);
        }

        Color drawingState;
        Color[] old;
        
        private void PadStarted(int index) {
            drawingState = (_pattern[_pattern.Expanded].Screen[LaunchpadGrid.GridToSignal(index)] == ColorPicker.Color)
                ? new Color(0)
                : ColorPicker.Color;
            
            old = (from i in _pattern[_pattern.Expanded].Screen select i.Clone()).ToArray();
        }
    
        private void PadPressed(int index, InputModifiers mods = InputModifiers.None) {
            if (Locked) return;

            int signalIndex = LaunchpadGrid.GridToSignal(index);

            if (mods.HasFlag(InputModifiers.Control)) {
                if (_pattern[_pattern.Expanded].Screen[signalIndex] != new Color(0)) {
                    Color color = _pattern[_pattern.Expanded].Screen[signalIndex];
                    ColorPicker.SetColor(color.Clone());
                    ColorHistory.Select(color.Clone(), true);
                }
                return;
            }

            if (_pattern[_pattern.Expanded].Screen[signalIndex] != ColorPicker.Color) Dispatcher.UIThread.InvokeAsync(() => {
                ColorHistory.Use();
            });

            _pattern[_pattern.Expanded].Screen[signalIndex] = drawingState.Clone();

            SolidColorBrush brush = (SolidColorBrush)_pattern[_pattern.Expanded].Screen[signalIndex].ToScreenBrush();

            Editor.SetColor(index, brush);
            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Launchpad.SetColor(index, brush);

            Launchpad?.Send(new Signal(Launchpad, (byte)signalIndex, _pattern[_pattern.Expanded].Screen[signalIndex]));
        }

        private void PadFinished(int _) {
            if (old == null) return;

            if (!old.SequenceEqual(_pattern[_pattern.Expanded].Screen)) {
                Color[] u = old;
                Color[] r = (from i in _pattern[_pattern.Expanded].Screen select i.Clone()).ToArray();
                int index = _pattern.Expanded;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {index} Changed", () => {
                    ((Pattern)Track.TraversePath(path))[index].Screen = (from i in u select i.Clone()).ToArray();
                }, () => {
                    ((Pattern)Track.TraversePath(path))[index].Screen = (from i in u select i.Clone()).ToArray();
                });
            }

            old = null;
        }

        public void SetGrid(int index, Frame frame) {
            if (_pattern.Expanded == index) {
                Editor.RenderFrame(frame);

                for (int i = 0; i < frame.Screen.Length; i++)
                    _launchpad?.Send(new Signal(Launchpad, (byte)i, frame.Screen[i]));
            }
        }

        private void HandleGesture(int x, int y) {
            if (x == -1 && y == 0) { // Left
                if (_pattern.Expanded == 0) Frame_Insert(0);
                else Frame_Select(_pattern.Expanded - 1);

            } else if (x == 1 && y == 0) { // Right
                if (_pattern.Expanded == _pattern.Count - 1) Frame_Insert(_pattern.Count);
                else Frame_Select(_pattern.Expanded + 1);

            } else if (x == 0 && y == 1) { // Up
                origin = -1;
                historyShowing = Locked = true;
                RenderHistory();
                
            } else if (x == 0 && y == -1) { // Down
                PadStarted(-1);
                PadPressed(-1);
                PadFinished(-1);
                
            } else if (x == -1 && y == 1) // Up-Left
                PatternPlay(null, null);
                
            else if (x == 1 && y == -1) // Down-Right
                PatternFire(null, null);
                
            else if (x == 1 && y == 1) // Up-Right
                Frame_Insert(_pattern.Expanded + 1);
                
            else if (x == -1 && y == -1) // Down-Left
                Frame_Remove(_pattern.Expanded);
        }

        public void MIDIEnter(Signal n) {
            if (!Preferences.EnableGestures) {
                if (n.Color.Lit) Dispatcher.UIThread.InvokeAsync(() => {
                    int index = LaunchpadGrid.SignalToGrid(n.Index);
                    PadStarted(index);
                    PadPressed(index);
                    PadFinished(index);
                });

                return;
            }

            if (n.Color.Lit) {
                if (historyShowing) {
                    int x = n.Index % 10;
                    int y = n.Index / 10;

                    if (x < 1 || 8 < x || y < 1 || 8 < y) return;
                    
                    int i = 64 - y * 8 + x - 1;

                    if (i < ColorHistory.Count) Dispatcher.UIThread.InvokeAsync(() => {
                        ColorHistory.Input(i);

                        historyShowing = Locked = false;
                        Frame_Select(_pattern.Expanded);
                    });

                } else if (origin == -1) {
                    origin = n.Index;
                    gestureUsed = false;

                } else if (gesturePoint == -1)
                    gesturePoint = n.Index;

            } else {
                if (historyShowing) return;

                if (n.Index == origin) {
                    if (!gestureUsed && !Locked) Dispatcher.UIThread.InvokeAsync(() => {
                        int index = LaunchpadGrid.SignalToGrid(n.Index);
                        PadStarted(index);
                        PadPressed(index);
                        PadFinished(index);
                    });

                    origin = gesturePoint;
                    gesturePoint = -1;

                } else if (n.Index == gesturePoint) {
                    int x = gesturePoint % 10 - origin % 10;
                    int y = gesturePoint / 10 - origin / 10;

                    if (!Locked) Dispatcher.UIThread.InvokeAsync(() => { HandleGesture(x, y); });

                    gestureUsed = true;
                    gesturePoint = -1;
                }
            }
        }

        private void Duration_Changed(double value, double? old) {
            if (old != null) {
                int u = (int)old.Value;
                int r = (int)value;
                int index = _pattern.Expanded;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {index} Duration Changed", () => {
                    ((Pattern)Track.TraversePath(path))[index].Time = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path))[index].Time = r;
                });
            }

            _pattern[_pattern.Expanded].Time = (int)value;
        }

        public void SetDurationValue(int index, int value) {
            if (_pattern.Expanded == index)
                Duration.RawValue = value;
        }

        private void Duration_StepChanged(int value, int? old) {
            if (old != null) {
                int u = old.Value;
                int r = value;
                int index = _pattern.Expanded;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {index} Duration Changed", () => {
                    ((Pattern)Track.TraversePath(path))[index].Length.Step = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path))[index].Length.Step = r;
                });
            }

            _pattern[_pattern.Expanded].Length.Step = value;
        }

        public void SetDurationStep(int index, int value) {
            if (_pattern.Expanded == index)
                Duration.DrawArcAuto();
        }

        private void Duration_ModeChanged(bool value, bool? old) {
            if (old != null) {
                bool u = old.Value;
                bool r = value;
                int index = _pattern.Expanded;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {index} Duration Switched", () => {
                    ((Pattern)Track.TraversePath(path))[index].Mode = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path))[index].Mode = r;
                });
            }

            _pattern[_pattern.Expanded].Mode = value;
        }

        public void SetDurationMode(int index, bool value) {
            if (_pattern.Expanded == index)
                Duration.UsingSteps = value;
        }

        private void Gate_Changed(double value, double? old) {
            if (old != null) {
                decimal u = (decimal)(old.Value / 100);
                decimal r = (decimal)(value / 100);
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Gate Changed", () => {
                    ((Pattern)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Gate = r;
                });
            }

            _pattern.Gate = (decimal)(value / 100);
        }

        public void SetGate(decimal gate) => Gate.RawValue = (double)gate * 100;

        private void PlaybackMode_Changed(object sender, SelectionChangedEventArgs e) {
            string selected = (string)PlaybackMode.SelectedItem;

            if (_pattern.Mode != selected) {
                string u = _pattern.Mode;
                string r = selected;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Playback Mode Changed", () => {
                    ((Pattern)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Mode = r;
                });

                _pattern.Mode = selected;
            }
        }

        public void SetPlaybackMode(string mode) => PlaybackMode.SelectedItem = mode;

        private void Choke_MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) {
                bool u = Choke.Enabled;
                bool r = !Choke.Enabled;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Choke Changed", () => {
                    Pattern pattern = ((Pattern)Track.TraversePath(path));
                    if (pattern.Window != null) {
                        pattern.Window.Choke.Enabled = u;
                        if (!pattern.Window.Choke.Enabled) pattern.Choke = null;
                    }

                }, () => {
                    Pattern pattern = ((Pattern)Track.TraversePath(path));
                    if (pattern.Window != null) {
                        pattern.Window.Choke.Enabled = r;
                        if (!pattern.Window.Choke.Enabled) pattern.Choke = null;
                    }
                });

                Choke.Enabled = !Choke.Enabled;
                if (!Choke.Enabled) _pattern.Choke = null;
            }
        }

        private void Choke_Changed(double value, double? old) {
            if (old != null) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Choke Changed", () => {
                    ((Pattern)Track.TraversePath(path)).Choke = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Choke = r;
                });
            }

            _pattern.Choke = (int)value;
        }

        public void SetChoke(int choke) => Choke.RawValue = choke;

        private void PlayColor(int index, Color color) {
            Dispatcher.UIThread.InvokeAsync(() => {
                Editor.SetColor(LaunchpadGrid.SignalToGrid(index), color.ToScreenBrush());
            });

            PlayExit?.Invoke(new Signal(_track.Launchpad, (byte)index, color.Clone()));
        }

        private void FireCourier(decimal time) {
            Courier courier;
            PlayTimers.Add(courier = new Courier() {
                AutoReset = false,
                Interval = (double)time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            if (!Locked) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            lock (PlayLocker) {
                if (++PlayIndex < _pattern.Count) {
                    for (int i = 0; i < _pattern[PlayIndex].Screen.Length; i++)
                        if (_pattern[PlayIndex].Screen[i] != _pattern[PlayIndex - 1].Screen[i])
                            PlayColor(i, _pattern[PlayIndex].Screen[i]);

                } else {
                    for (int i = 0; i < _pattern.Frames.Last().Screen.Length; i++)
                        if (_pattern.Frames.Last().Screen[i].Lit)
                            PlayColor(i, new Color(0));

                    if (_launchpad != null) PlayExit = Launchpad.Send;
                    else PlayExit = null;

                    Dispatcher.UIThread.InvokeAsync(() => {
                        Locked = false;

                        Editor.RenderFrame(_pattern[_pattern.Expanded]);
                    });

                    for (int i = 0; i < _pattern[_pattern.Expanded].Screen.Length; i++)
                        Launchpad?.Send(new Signal(_track.Launchpad, (byte)i, _pattern[_pattern.Expanded].Screen[i]));
                }

                PlayTimers.Remove(courier);
            }
        }

        private int PlayIndex = 0;
        private object PlayLocker = new object();
        private List<Courier> PlayTimers = new List<Courier>();

        private void PatternStop() {
            lock (PlayLocker) {
                for (int i = 0; i < PlayTimers.Count; i++)
                    PlayTimers[i].Dispose();
                
                if (PlayIndex < _pattern.Count)
                    for (int i = 0; i < _pattern[PlayIndex].Screen.Length; i++)
                        if (_pattern[PlayIndex].Screen[i].Lit)
                            PlayColor(i, new Color(0));

                PlayTimers = new List<Courier>();
                PlayIndex = 0;
            }
        }

        private void PatternPlay(object sender, RoutedEventArgs e) {
            if (Locked) return;
            Locked = true;

            PatternStop();
            Editor.RenderFrame(_pattern[0]);

            for (int i = 0; i < _pattern[0].Screen.Length; i++)
                PlayExit?.Invoke(new Signal(Launchpad, (byte)i, _pattern[0].Screen[i].Clone()));

            decimal time = 0;

            for (int i = 0; i < _pattern.Count; i++) {
                time += (_pattern[i].Mode? (int)_pattern[i].Length : _pattern[i].Time) * _pattern.Gate;
                FireCourier(time);
            }
        }

        private void PatternFire(object sender, RoutedEventArgs e) {
            if (Locked) return;

            PlayExit = _pattern.MIDIExit;
            PatternPlay(sender, e);
        }

        private static void ImportFrames(Pattern pattern, List<Frame> frames, decimal gate) {
            pattern.Frames = frames;
            pattern.Gate = gate;

            while (pattern.Window?.Contents.Count > 1) pattern.Window?.Contents.RemoveAt(1);
            pattern.Expanded = 0;

            for (int i = 0; i < pattern.Count; i++)
                pattern.Window?.Contents_Insert(i, pattern[i], true);

            if (pattern.Count == 1) ((FrameDisplay)pattern.Window?.Contents[1]).Remove.Opacity = 0;

            pattern.Window?.Frame_Select(0);
        }

        private void ImportFile(string filepath) {
            if (!Importer.FramesFromMIDI(filepath, out List<Frame> frames) &&
                !Importer.FramesFromImage(filepath, out frames))
                return;

            List<Frame> uf = _pattern.Frames.ToList();
            decimal ug = _pattern.Gate;
            List<Frame> rf = frames.ToList();
            decimal rg = 1;
            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Pattern File Imported", () => {
                ImportFrames((Pattern)Track.TraversePath(path), uf.ToList(), ug);
            }, () => {
                ImportFrames((Pattern)Track.TraversePath(path), rf.ToList(), rg);
            });

            ImportFrames(_pattern, frames, 1);
        }

        private async void ImportDialog(object sender, RoutedEventArgs e) {
            if (Locked) return;

            OpenFileDialog ofd = new OpenFileDialog() {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "mid",
                            "gif"
                        },
                        Name = "All Supported Files"
                    },
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "mid"
                        },
                        Name = "MIDI Files"
                    },
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "gif"
                        },
                        Name = "GIF Images"
                    }
                },
                Title = "Import Pattern"
            };

            string[] result = await ofd.ShowAsync(this);
            if (result.Length > 0) ImportFile(result[0]);
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("frame")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("frame")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneAfter" && source.Name != "FrameAdd")
                source = source.Parent;

            List<Frame> moving = ((List<ISelect>)e.Data.Get("frame")).Select(i => (Frame)i).ToList();

            Pattern source_parent = moving[0].Parent;

            int before = moving[0].IParentIndex.Value - 1;
            int after = (source.Name == "DropZoneAfter")? _pattern.Count - 1 : -1;

            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result = Frame.Move(moving, _pattern, after, copy);

            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == _pattern && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_parent);
                List<int> targetpath = Track.GetPath(_pattern);
                
                Program.Project.Undo.Add(copy? $"Frame Copied" : $"Frame Moved", copy
                    ? new Action(() => {
                        Pattern targetpattern = ((Pattern)Track.TraversePath(targetpath));

                        for (int i = after + count; i > after; i--)
                            targetpattern.Remove(i);

                    }) : new Action(() => {
                        Pattern sourcepattern = ((Pattern)Track.TraversePath(sourcepath));
                        Pattern targetpattern = ((Pattern)Track.TraversePath(targetpath));

                        List<Frame> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetpattern[i]).ToList();

                        Frame.Move(umoving, sourcepattern, before_pos);

                }), () => {
                    Pattern sourcepattern = ((Pattern)Track.TraversePath(sourcepath));
                    Pattern targetpattern = ((Pattern)Track.TraversePath(targetpath));

                    List<Frame> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcepattern[i]).ToList();

                    Frame.Move(rmoving, targetpattern, after, copy);
                });
            
            } else e.DragEffects = DragDropEffects.None;
        }

        public async void Copy(int left, int right, bool cut = false) {
            if (Locked) return;

            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_pattern[i]);

            string b64 = Convert.ToBase64String(Encoder.Encode(copy).ToArray());

            if (cut) Delete(left, right);
            
            await Application.Current.Clipboard.SetTextAsync(b64);
        }

        public async void Paste(int right) {
            if (Locked) return;

            string b64 = await Application.Current.Clipboard.GetTextAsync();
            
            Copyable paste = Decoder.Decode(new MemoryStream(Convert.FromBase64String(b64)), typeof(Copyable));
            
            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Frame Pasted", () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    pattern.Remove(right + i + 1);

            }, () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                for (int i = 0; i < paste.Contents.Count; i++)
                    pattern.Insert(right + i + 1, ((Frame)paste.Contents[i]).Clone());
            });

            for (int i = 0; i < paste.Contents.Count; i++)
                _pattern.Insert(right + i + 1, ((Frame)paste.Contents[i]).Clone());
        }

        public void Duplicate(int left, int right) {
            if (Locked) return;

            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Frame Duplicated", () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                for (int i = right - left; i >= 0; i--)
                    pattern.Remove(right + i + 1);

            }, () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                for (int i = 0; i <= right - left; i++)
                    pattern.Insert(right + i + 1, pattern[left + i].Clone());
            });

            for (int i = 0; i <= right - left; i++)
                _pattern.Insert(right + i + 1, _pattern[left + i].Clone());
        }

        public void Delete(int left, int right) {
            if (Locked) return;

            if (_pattern.Count - (right - left + 1) == 0) return;

            List<Frame> u = (from i in Enumerable.Range(left, right - left + 1) select _pattern[i].Clone()).ToList();

            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Frame Deleted", () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    pattern.Insert(i, u[i - left].Clone());

            }, () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                for (int i = right; i >= left; i--)
                    pattern.Remove(i);
            });

            for (int i = right; i >= left; i--)
                _pattern.Remove(i);
        }

        public void Group(int left, int right) => throw new InvalidOperationException("A Frame cannot be grouped.");
        public void Ungroup(int index) => throw new InvalidOperationException("A Frame cannot be ungrouped.");
        public void Rename(int left, int right) => throw new InvalidOperationException("A Frame cannot be renamed.");

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        public static void Create(Pattern pattern, Window owner) {
            if (pattern.Window == null) {
                pattern.Window = new PatternWindow(pattern) {Owner = owner};
                pattern.Window.Show();
                pattern.Window.Owner = null;
            } else {
                pattern.Window.WindowState = WindowState.Normal;
                pattern.Window.Activate();
            }
        }
    }
}