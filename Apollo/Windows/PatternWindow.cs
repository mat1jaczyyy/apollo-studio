using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Interfaces;
using Apollo.Structures;

namespace Apollo.Windows {
    public class PatternWindow: Window, ISelectParentViewer {
        public int? IExpanded {
            get => _pattern.Expanded;
        }

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            TitleText = this.Get<TextBlock>("Title");
            TitleCenter = this.Get<TextBlock>("TitleCenter");
            
            CenteringLeft = this.Get<StackPanel>("CenteringLeft");
            CenteringRight = this.Get<StackPanel>("CenteringRight");

            UndoButton = this.Get<UndoButton>("UndoButton");
            RedoButton = this.Get<RedoButton>("RedoButton");

            FrameList = this.Get<ScrollViewer>("FrameList");
            Editor = this.Get<LaunchpadGrid>("Editor");
            RootKey = this.Get<LaunchpadGrid>("RootKey");
            Wrap = this.Get<CheckBox>("Wrap");

            Duration = this.Get<Dial>("Duration");
            Gate = this.Get<Dial>("Gate");
            Repeats = this.Get<Dial>("Repeats");
            Pinch = this.Get<Dial>("Pinch");

            PlaybackMode = this.Get<ComboBox>("PlaybackMode");
            Infinite = this.Get<CheckBox>("Infinite");

            ImportButton = this.Get<Button>("Import");
            Play = this.Get<Button>("Play");
            Fire = this.Get<Button>("Fire");

            Reverse = this.Get<Button>("Reverse");
            Invert = this.Get<Button>("Invert");

            Contents = this.Get<StackPanel>("Frames").Children;
            PortSelector = this.Get<ComboBox>("PortSelector");

            ColorPicker = this.Get<ColorPicker>("ColorPicker");
            ColorHistory = this.Get<ColorHistory>("ColorHistory");
        }

        HashSet<IDisposable> observables = new HashSet<IDisposable>();

        Pattern _pattern;
        Track _track;

        public Pattern Device { get => _pattern; }
        
        Launchpad _launchpad;
        Launchpad Launchpad {
            get => _launchpad;
            set {
                if (_launchpad != null) {
                    _launchpad.PatternWindow = null;
                    _launchpad.Clear();
                }

                _launchpad = value;

                if (_launchpad != null && _launchpad != MIDI.NoOutput) {
                    _launchpad.PatternWindow?.Close();
                    _launchpad.PatternWindow = this;
                    _launchpad.Clear();
                    PlayExit = _launchpad.Send;
                } else PlayExit = null;

                origin = gesturePoint = -1;

                if (historyShowing) RenderHistory();
                else if (IsArrangeValid)
                    for (int i = 0; i < _pattern[_pattern.Expanded].Screen.Length; i++)
                        _launchpad?.Send(new Signal(this, Launchpad, (byte)i, _pattern[_pattern.Expanded].Screen[i]));
            }
        }

        TextBlock TitleText, TitleCenter;
        StackPanel CenteringLeft, CenteringRight;
        UndoButton UndoButton;
        RedoButton RedoButton;
        ScrollViewer FrameList;
        ComboBox PortSelector, PlaybackMode;
        LaunchpadGrid Editor, RootKey;
        Controls Contents;
        ContextMenu FrameContextMenu;
        ColorPicker ColorPicker;
        ColorHistory ColorHistory;
        Dial Duration, Gate, Repeats, Pinch;
        Button ImportButton, Play, Fire, Reverse, Invert;
        CheckBox Wrap, Infinite;

        int origin = -1;
        int gesturePoint = -1;
        bool gestureUsed = false;
        bool historyShowing = false;

        bool _locked = false;
        bool Locked {
            get => _locked;
            set {
                _locked = value;

                if (Repeats.Enabled) Repeats.DisplayDisabledText = false;
                if (Gate.Enabled) Gate.DisplayDisabledText = false;
                if (Duration.Enabled) Duration.DisplayDisabledText = false;

                UndoButton.IsEnabled =
                RedoButton.IsEnabled =
                ImportButton.IsEnabled =
                Repeats.Enabled =
                Gate.Enabled =
                PlaybackMode.IsEnabled =
                Wrap.IsEnabled =
                Infinite.IsEnabled =
                Play.IsEnabled =
                Fire.IsEnabled =
                PortSelector.IsEnabled =
                ColorPicker.IsEnabled =
                ColorHistory.IsEnabled =
                Duration.Enabled =
                Duration.IsEnabled =
                Reverse.IsEnabled =
                Invert.IsEnabled = !_locked;

                if (!_locked) {
                    Duration.Enabled = !(_pattern.Infinite && _pattern.Expanded == _pattern.Count - 1);
                    Repeats.Enabled = !(_pattern.Infinite || _pattern.Mode == PlaybackType.Loop);
                    Repeats.DisabledText = (_pattern.Mode == PlaybackType.Loop)? "Infinite" : "1";

                    Repeats.DisplayDisabledText = Gate.DisplayDisabledText = Duration.DisplayDisabledText = true;
                }
            }
        }

        Action<Signal> PlayExit;
        
        void UpdateTitle() => UpdateTitle(_track.ParentIndex.Value, _track.ProcessedName);
        void UpdateTitle(int index) => UpdateTitle(index, _track.ProcessedName);
        void UpdateTitle(string name) => UpdateTitle(_track.ParentIndex.Value, name);
        void UpdateTitle(int index, string name)
            => Title = TitleText.Text = TitleCenter.Text = $"Editing Pattern - {name}";

        void UpdateTopmost(bool value) => Topmost = value;

        void UpdatePorts() {
            List<Launchpad> ports = (from i in MIDI.Devices where i.Available && i.Type != LaunchpadType.Unknown select i).ToList();
            if (Launchpad != null && (!Launchpad.Available || Launchpad.Type == LaunchpadType.Unknown)) ports.Add(Launchpad);
            ports.Add(MIDI.NoOutput);

            PortSelector.Items = ports;
            PortSelector.SelectedIndex = -1;
            PortSelector.SelectedItem = Launchpad;
        }

        void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);
        
        void SetAlwaysShowing() {
            for (int i = 1; i < Contents.Count; i++)
                ((FrameDisplay)Contents[i]).FrameAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((FrameDisplay)Contents.Last()).FrameAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Frame frame, bool ignoreExpanded = false) {
            if (Contents.Count > 1) {
                ((FrameDisplay)Contents[1]).Remove.Opacity = 1;
                ((FrameDisplay)Contents[Contents.Count - 1]).Viewer.Time.Text = _pattern.Frames[Contents.Count - 2].Time.ToString();
            }

            FrameDisplay viewer = new FrameDisplay(frame, _pattern);
            viewer.FrameAdded += Frame_Insert;
            viewer.FrameRemoved += Frame_Remove;
            viewer.FrameSelected += Frame_Select;
            frame.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();

            ((FrameDisplay)Contents[Contents.Count - 1]).Viewer.Time.Text = _pattern.Frames[Contents.Count - 2].ToString();

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

        public SelectionManager Selection;

        public PatternWindow() => new InvalidOperationException();

        public PatternWindow(Pattern pattern) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _pattern = pattern;
            _pattern.MIDIEnter(new StopSignal());

            _track = Track.Get(_pattern);

            observables.Add(Editor.GetObservable(Visual.BoundsProperty).Subscribe(Editor_Updated));

            SetRootKey(_pattern.RootKey);
            Wrap.IsChecked = _pattern.Wrap;

            Repeats.RawValue = _pattern.Repeats;
            Gate.RawValue = _pattern.Gate * 100;
            Pinch.RawValue = _pattern.Pinch;

            PlaybackMode.SelectedIndex = (int)_pattern.Mode;

            Infinite.IsChecked = _pattern.Infinite;

            FrameContextMenu = (ContextMenu)this.Resources["FrameContextMenu"];
            FrameContextMenu.AddHandler(MenuItem.ClickEvent, FrameContextMenu_Click);

            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            this.AddHandler(DragDrop.DropEvent, Drop);

            for (int i = 0; i < _pattern.Count; i++)
                Contents_Insert(i, _pattern[i], true);
            
            if (_pattern.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;

            Selection = new SelectionManager(() => _pattern.Frames.FirstOrDefault());

            Launchpad = Preferences.CaptureLaunchpad? _track.Launchpad : MIDI.NoOutput;

            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            ColorPicker.SetColor(ColorHistory.GetColor(0)?? new Color());
            ColorHistory.Select(ColorPicker.Color.Clone(), true);
            
            Frame_Select(_pattern.Expanded);
            Selection.Select(_pattern[_pattern.Expanded]);

            observables.Add(this.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(TitleText.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(TitleCenter.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(CenteringLeft.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(CenteringRight.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
        }

        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            _track.ParentIndexChanged += UpdateTitle;
            _track.NameChanged += UpdateTitle;
            UpdateTitle();

            ColorHistory.HistoryChanged += RenderHistory;
        }

        void Unloaded(object sender, CancelEventArgs e) {
            Locked = false;
            
            foreach (Courier i in PlayTimers) i.Dispose();
            PlayTimers.Clear();

            foreach (Frame frame in _pattern.Frames)
                frame.Info = null;

            _pattern.Window = null;
            _pattern = null;

            if (Launchpad != null) {
                Launchpad.PatternWindow = null;
                Launchpad.Clear();
            }

            _launchpad = null;

            MIDI.DevicesUpdated -= HandlePorts;

            Selection.Dispose();
            
            _track.ParentIndexChanged -= UpdateTitle;
            _track.NameChanged -= UpdateTitle;
            _track = null;

            FrameContextMenu.RemoveHandler(MenuItem.ClickEvent, FrameContextMenu_Click);
            FrameContextMenu = null;

            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
            this.RemoveHandler(DragDrop.DropEvent, Drop);

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            ColorHistory.HistoryChanged -= RenderHistory;

            foreach (IDisposable observable in observables)
                observable.Dispose();

            this.Content = null;

            App.WindowClosed(this);
        }

        public void Bounds_Updated(Rect bounds) {
            if (Bounds.IsEmpty || TitleText.Bounds.IsEmpty || TitleCenter.Bounds.IsEmpty || CenteringLeft.Bounds.IsEmpty || CenteringRight.Bounds.IsEmpty) return;

            int result = Convert.ToInt32((Bounds.Width - TitleText.Bounds.Width) / 2 <= Math.Max(CenteringLeft.Bounds.Width, CenteringRight.Bounds.Width) + 10);

            TitleText.Opacity = result;
            TitleCenter.Opacity = 1 - result;
        }

        public void Editor_Updated(Rect bounds) {
            if (bounds.IsEmpty) return;

            Editor.Scale = Math.Min(bounds.Width, bounds.Height) / 189.6;
        }

        void Port_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)PortSelector.SelectedItem;

            if (selected != null && Launchpad != selected) {
                Launchpad = selected;
                UpdatePorts();
            }
        }

        public void Expand(int? index) {
            if (index != null) Frame_Select(index.Value);
        }

        void Frame_Insert(int index) {
            if (Locked) return;

            Frame reference = _pattern[Math.Max(0, index - 1)];

            Frame_Insert(index, new Frame(reference.Time.Clone()));
        }

        void Frame_Insert(int index, Frame frame) {
            if (Locked) return;

            Frame reference = _pattern[Math.Max(0, index - 1)];

            if (Preferences.CopyPreviousFrame)
                for (int i = 0; i < reference.Screen.Length; i++)
                    frame.Screen[i] = reference.Screen[i].Clone();
            
            Frame r = frame.Clone();
            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Pattern Frame {index + 1} Inserted", () => {
                ((Pattern)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Pattern)Track.TraversePath(path)).Insert(index, r.Clone());
            }, () => {
                r.Dispose();
            });

            _pattern.Insert(index, frame);
        }

        void Frame_InsertStart() => Frame_Insert(0);

        void Frame_Remove(int index) {
            if (Locked) return;

            if (_pattern.Count == 1) return;

            Frame u = _pattern[index].Clone();
            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Pattern Frame {index + 1} Removed", () => {
                ((Pattern)Track.TraversePath(path)).Insert(index, u.Clone());
            }, () => {
                ((Pattern)Track.TraversePath(path)).Remove(index);
            }, () => {
                u.Dispose();
            });

            _pattern.Remove(index);
        }

        public bool Draw = true;

        public void Frame_Select(int index) {
            if (Locked) return;

            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.FontWeight = FontWeight.Normal;

            _pattern.Expanded = index;

            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.FontWeight = FontWeight.Bold;
            
            Duration.UsingSteps = _pattern[_pattern.Expanded].Time.Mode;
            Duration.Length = _pattern[_pattern.Expanded].Time.Length;
            Duration.RawValue = _pattern[_pattern.Expanded].Time.Free;

            Duration.Enabled = !(_pattern.Infinite && _pattern.Expanded == _pattern.Count - 1);
            Repeats.Enabled = !(_pattern.Infinite || _pattern.Mode == PlaybackType.Loop);
            Repeats.DisabledText = (_pattern.Mode == PlaybackType.Loop)? "Infinite" : "1";

            if (Draw) {
                Editor.RenderFrame(_pattern[_pattern.Expanded]);

                for (int i = 0; i < _pattern[_pattern.Expanded].Screen.Length; i++)
                    Launchpad?.Send(new Signal(this, Launchpad, (byte)i, _pattern[_pattern.Expanded].Screen[i]));
            }
        }

        void Frame_Action(string action) => Frame_Action(action, false);
        void Frame_Action(string action, bool right) => Selection.Action(action, _pattern, (right? _pattern.Count : 0) - 1);

        void FrameContextMenu_Click(object sender, EventArgs e) {
            this.Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Frame_Action((string)((MenuItem)item).Header, true);
        }
        
        void Frame_AfterClick(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
                FrameContextMenu.Open((Control)sender);

            e.Handled = true;
        }

        async void HandleKey(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Enter || e.Key == Key.Space) {
                if (e.KeyModifiers == KeyModifiers.Shift) PatternFire(Fire, null);
                else if (e.KeyModifiers == KeyModifiers.None) PatternPlay(Play, null);
                else PatternPlay(Play, null);
            }

            if (Locked) return;
            
            if (e.Key == Key.Insert || e.Key == Key.Add || e.Key == Key.OemPlus) Frame_Insert(_pattern.Expanded + 1);
            else if (e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Subtract || e.Key == Key.OemMinus) Selection.Action("Delete");

            else {
                if (App.WindowKey(this, e) || await Program.Project.HandleKey(this, e) || Program.Project.Undo.HandleKey(e) || Selection.HandleKey(e)) {
                    this.Focus();
                    return;
                }

                if (e.KeyModifiers != KeyModifiers.None && e.KeyModifiers != KeyModifiers.Shift) return;

                bool shift = e.KeyModifiers == KeyModifiers.Shift;

                if (e.Key == Key.Up || e.Key == Key.Left) {
                    if (Selection.Move(false, shift) || shift) Frame_Select(Selection.Start.IParentIndex.Value);
                    else Frame_Insert(0);

                } else if (e.Key == Key.Down || e.Key == Key.Right) {
                    if (Selection.Move(true, shift) || shift) Frame_Select(Selection.Start.IParentIndex.Value);
                    else Frame_Insert(_pattern.Count);

                } else if (e.Key == Key.Home) {
                    Selection.Select(_pattern[0], shift);
                    Frame_Select(0);

                } else if (e.Key == Key.End) {
                    Selection.Select(_pattern[_pattern.Count - 1], shift);
                    Frame_Select(_pattern.Count - 1);

                } else if (e.Key == Key.PageUp) {
                    int target = Math.Max(0, Selection.Start.IParentIndex.Value - (int)(FrameList.Bounds.Height / Contents[1].Bounds.Height));
                    Selection.Select(_pattern[target], shift);
                    Frame_Select(target);
                    
                } else if (e.Key == Key.PageDown) {
                    int target = Math.Min(_pattern.Count - 1, Selection.Start.IParentIndex.Value + (int)(FrameList.Bounds.Height / Contents[1].Bounds.Height));
                    Selection.Select(_pattern[target], shift);
                    Frame_Select(target);
                }
            }
        }

        void Window_KeyDown(object sender, KeyEventArgs e) {
            List<Window> windows = App.Windows.ToList();
            HandleKey(sender, e);
            
            if (windows.SequenceEqual(App.Windows) && FocusManager.Instance.Current?.GetType() != typeof(TextBox))
                this.Focus();
        }

        void ColorPicker_Changed(Color color, Color old) => ColorHistory.Select(color.Clone());

        void ColorHistory_Changed(Color color) {
            ColorPicker.SetColor(color.Clone());
            RenderHistory();
        }

        void RenderHistory() {
            if (!historyShowing) return;
            ColorHistory.Render(Launchpad, this);
        }

        Color drawingState;
        Color[] oldScreen;
        
        void PadStarted(int index) {
            drawingState = (_pattern[_pattern.Expanded].Screen[LaunchpadGrid.GridToSignal(index)] == ColorPicker.Color)
                ? new Color(0)
                : ColorPicker.Color;
            
            oldScreen = (from i in _pattern[_pattern.Expanded].Screen select i.Clone()).ToArray();
        }
    
        void PadPressed(int index, KeyModifiers mods = KeyModifiers.None) {
            if (Locked) return;

            int signalIndex = LaunchpadGrid.GridToSignal(index);

            if (mods.HasFlag(App.ControlKey)) {
                Color color = _pattern[_pattern.Expanded].Screen[signalIndex];
                ColorPicker.SetColor(color.Clone());
                ColorHistory.Select(color.Clone(), true);
                return;
            }

            if (_pattern[_pattern.Expanded].Screen[signalIndex] != ColorPicker.Color) Dispatcher.UIThread.InvokeAsync(() => {
                ColorHistory.Use();
            });

            _pattern[_pattern.Expanded].Screen[signalIndex] = drawingState.Clone();

            SolidColorBrush brush = (SolidColorBrush)_pattern[_pattern.Expanded].Screen[signalIndex].ToScreenBrush();

            Editor.SetColor(index, brush);
            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Launchpad.SetColor(index, brush);

            Launchpad?.Send(new Signal(this, Launchpad, (byte)signalIndex, _pattern[_pattern.Expanded].Screen[signalIndex]));
        }

        void PadFinished(int _) {
            if (oldScreen == null) return;

            if (!oldScreen.SequenceEqual(_pattern[_pattern.Expanded].Screen)) {
                Color[] u = oldScreen;
                Color[] r = (from i in _pattern[_pattern.Expanded].Screen select i.Clone()).ToArray();
                int index = _pattern.Expanded;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {index + 1} Changed", () => {
                    ((Pattern)Track.TraversePath(path))[index].Screen = (from i in u select i.Clone()).ToArray();
                }, () => {
                    ((Pattern)Track.TraversePath(path))[index].Screen = (from i in r select i.Clone()).ToArray();
                });
            }

            oldScreen = null;
        }

        public void SetGrid(int index, Frame frame) {
            if (_pattern.Expanded == index) {
                Editor.RenderFrame(frame);

                for (int i = 0; i < frame.Screen.Length; i++)
                    _launchpad?.Send(new Signal(this, Launchpad, (byte)i, frame.Screen[i]));
            }
        }

        void HandleGesture(int x, int y) {
            if (x == -1 && y == 0) { // Left
                if (_pattern.Expanded == 0) Frame_Insert(0);
                else Frame_Select(_pattern.Expanded - 1);

            } else if (x == 1 && y == 0) { // Right
                if (_pattern.Expanded == _pattern.Count - 1) Frame_Insert(_pattern.Count);
                else Frame_Select(_pattern.Expanded + 1);

            } else if (x == 0 && y == 1) { // Up
                if (Locked) return;
                origin = -1;
                historyShowing = Locked = true;
                RenderHistory();
                
            } else if (x == 0 && y == -1) { // Down
                if (Locked) return;
                PadStarted(-1);
                PadPressed(-1);
                PadFinished(-1);
                
            } else if (x == -1 && y == 1) // Up-Left
                PatternPlay(Play, null);
                
            else if (x == 1 && y == -1) // Down-Right
                PatternFire(Fire, null);
                
            else if (x == 1 && y == 1) // Up-Right
                Frame_Insert(_pattern.Expanded + 1);
                
            else if (x == -1 && y == -1) // Down-Left
                Frame_Remove(_pattern.Expanded);
        }

        public void MIDIEnter(Signal n) {
            if (n.Source != Launchpad || n.Index == 100) return;

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

                    Dispatcher.UIThread.InvokeAsync(() => { HandleGesture(x, y); });

                    gestureUsed = true;
                    gesturePoint = -1;
                }
            }
        }

        void Infinite_Changed(object sender, RoutedEventArgs e) {
            bool value = Infinite.IsChecked.Value;

            if (_pattern.Infinite != value) {
                bool u = _pattern.Infinite;
                bool r = value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Infinite Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Pattern)Track.TraversePath(path)).Infinite = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Infinite = r;
                });

                _pattern.Infinite = value;
            }
        }

        public void SetInfinite(bool value) {
            Infinite.IsChecked = value;

            ((FrameDisplay)Contents.Last()).Viewer.Time.Text = _pattern.Frames.Last().ToString();

            Duration.Enabled = !(value && _pattern.Expanded == _pattern.Count - 1);
            Repeats.Enabled = !(_pattern.Infinite || _pattern.Mode == PlaybackType.Loop);
        }

        List<Time> oldTime;

        void Duration_Started() {
            oldTime = new List<Time>();

            foreach (Frame frame in Selection.Selection)
                oldTime.Add(frame.Time.Clone());
        }

        void Duration_Changed(Dial sender, double value, double? old) {
            if (oldTime == null) return;

            if (old != null && !oldTime.SequenceEqual((from i in Selection.Selection select ((Frame)i).Time.Clone()).ToList())) {
                int left = Selection.Selection[0].IParentIndex.Value;

                List<Time> u = oldTime.ToList();
                int r = (int)value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {_pattern.Expanded + 1} Duration Changed to {r}{Duration.Unit}", () => {
                    Pattern pattern = (Pattern)Track.TraversePath(path);
                    
                    for (int i = 0; i < u.Count; i++)
                        pattern[left + i].Time = u[i].Clone();
                    
                }, () => {
                    Pattern pattern = (Pattern)Track.TraversePath(path);
                    
                    for (int i = 0; i < u.Count; i++) {
                        pattern[left + i].Time.Free = r;
                        pattern[left + i].Time.Mode = false;
                    }
                
                }, () => {
                    foreach (Time time in u) time.Dispose();
                    u = null;
                });

                oldTime = null;
            }

            foreach (Frame frame in Selection.Selection) {
                frame.Time.Free = (int)value;
                frame.Time.Mode = false;
            }
        }

        public void SetDurationValue(int index, int value) {
            if (_pattern.Expanded == index)
                Duration.RawValue = value;
        }

        void Duration_StepChanged(int value, int? old) {
            if (oldTime == null) return;

            if (old != null && !oldTime.SequenceEqual((from i in Selection.Selection select ((Frame)i).Time.Clone()).ToList())) {
                int left = Selection.Selection[0].IParentIndex.Value;

                List<Time> u = oldTime.ToList();
                int r = value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {_pattern.Expanded + 1} Duration Changed to {Length.Steps[r]}", () => {
                    Pattern pattern = (Pattern)Track.TraversePath(path);
                    
                    for (int i = 0; i < u.Count; i++)
                        pattern[left + i].Time = u[i].Clone();
                    
                }, () => {
                    Pattern pattern = (Pattern)Track.TraversePath(path);
                    
                    for (int i = 0; i < u.Count; i++) {
                        pattern[left + i].Time.Length.Step = r;
                        pattern[left + i].Time.Mode = true;
                    }
                
                }, () => {
                    foreach (Time time in u) time.Dispose();
                    u = null;
                });

                oldTime = null;
            }

            foreach (Frame frame in Selection.Selection) {
                frame.Time.Length.Step = value;
                frame.Time.Mode = true;
            }
        }

        public void SetDurationStep(int index, Length value) {
            if (_pattern.Expanded == index)
                Duration.Length = value;
        }

        void Duration_ModeChanged(bool value, bool? old) {
            if (oldTime == null) return;

            if (old != null && !oldTime.SequenceEqual((from i in Selection.Selection select ((Frame)i).Time.Clone()).ToList())) {
                int left = Selection.Selection[0].IParentIndex.Value;

                List<Time> u = oldTime.ToList();
                bool r = value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Frame {_pattern.Expanded + 1} Duration Switched to {(r? "Steps" : "Free")}", () => {
                    Pattern pattern = (Pattern)Track.TraversePath(path);
                    
                    for (int i = 0; i < u.Count; i++)
                        pattern[left + i].Time = u[i].Clone();
                    
                }, () => {
                    Pattern pattern = (Pattern)Track.TraversePath(path);
                    
                    for (int i = 0; i < u.Count; i++)
                        pattern[left + i].Time.Mode = r;
                
                }, () => {
                    foreach (Time time in u) time.Dispose();
                    u = null;
                });

                oldTime = null;
            }

            foreach (Frame frame in Selection.Selection)
                frame.Time.Mode = value;
        }

        public void SetDurationMode(int index, bool value) {
            if (_pattern.Expanded == index)
                Duration.UsingSteps = value;
        }

        void Frame_Reverse(object sender, RoutedEventArgs e) {
            List<ISelect> selection = Selection.Selection;

            int left = selection.First().IParentIndex.Value;
            int right = selection.Last().IParentIndex.Value;

            if (left == right) return;

            int expanded = _pattern.Expanded;

            List<int> path = Track.GetPath(_pattern);

            void ur() {
                Pattern pattern = (Pattern)Track.TraversePath(path);

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = left; i < right; i++) {
                    Frame frame = pattern[right];
                    pattern.Remove(right);
                    pattern.Insert(i, frame);
                }

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(expanded);
                    pattern.Window.Selection.Select(pattern[left]);
                    pattern.Window.Selection.Select(pattern[right], true);
                }
            }

            Program.Project.Undo.Add($"Pattern Frames Reversed", ur, ur);

            Draw = false;

            for (int i = left; i < right; i++) {
                Frame frame = _pattern[right];
                _pattern.Remove(right);
                _pattern.Insert(i, frame);
            }

            Draw = true;

            Frame_Select(expanded);
            Selection.Select(_pattern[left]);
            Selection.Select(_pattern[right], true);
        }

        void Frame_Invert(object sender, RoutedEventArgs e) {
            List<ISelect> selection = Selection.Selection;

            int left = selection.First().IParentIndex.Value;
            int right = selection.Last().IParentIndex.Value;

            List<int> path = Track.GetPath(_pattern);

            void ur() {
                Pattern pattern = (Pattern)Track.TraversePath(path);

                for (int i = left; i <= right; i++)
                    pattern[i].Invert();
            }

            Program.Project.Undo.Add($"Pattern Frames Inverted", ur, ur);
            
            for (int i = left; i <= right; i++)
                _pattern[i].Invert();
        }

        void PlaybackMode_Changed(object sender, SelectionChangedEventArgs e) {
            PlaybackType selected = (PlaybackType)PlaybackMode.SelectedIndex;

            if (_pattern.Mode != selected) {
                PlaybackType u = _pattern.Mode;
                PlaybackType r = selected;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Playback Mode Changed to {r}", () => {
                    ((Pattern)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Mode = r;
                });

                _pattern.Mode = selected;
            }
        }

        public void SetPlaybackMode(PlaybackType mode) {
            PlaybackMode.SelectedIndex = (int)mode;

            Repeats.Enabled = !(_pattern.Infinite || _pattern.Mode == PlaybackType.Loop);
            Repeats.DisabledText = (_pattern.Mode == PlaybackType.Loop)? "Infinite" : "1";
        }

        void Gate_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Gate Changed to {value}{Gate.Unit}", () => {
                    ((Pattern)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Gate = r;
                });
            }

            _pattern.Gate = value / 100;
        }

        public void SetGate(double gate) => Gate.RawValue = gate * 100;

        void Repeats_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Repeats Changed to {value}{Repeats.Unit}", () => {
                    ((Pattern)Track.TraversePath(path)).Repeats = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Repeats = r;
                });
            }

            _pattern.Repeats = (int)value;
        }

        public void SetRepeats(int repeats) => Repeats.RawValue = repeats;

        void Pinch_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value;
                double r = value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Pinch Changed to {value}{Pinch.Unit}", () => {
                    ((Pattern)Track.TraversePath(path)).Pinch = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Pinch = r;
                });
            }

            _pattern.Pinch = value;
        }

        public void SetPinch(double pinch) => Pinch.RawValue = pinch;

        int? oldRootKey = -1;

        void RootKeyStarted(int index) {
            if (Locked) return;

            oldRootKey = _pattern.RootKey;
        }

        void RootKeyPressed(int index) {
            if (Locked) return;

            int signalIndex = LaunchpadGrid.GridToSignal(index);

            _pattern.RootKey = (signalIndex == _pattern.RootKey)? null : (int?)signalIndex;
        }

        void RootKeyFinished(int _) {
            if (Locked) return;

            if (oldRootKey == -1) return;

            if (oldRootKey != _pattern.RootKey) {
                int? u = oldRootKey;
                int? r = _pattern.RootKey;
                
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Root Key Changed", () => {
                    ((Pattern)Track.TraversePath(path)).RootKey = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).RootKey = r;
                });
            }

            oldRootKey = -1;
        }

        public void SetRootKey(int? index) {
            RootKey.Clear();

            if (index != null) RootKey.SetColor(
                LaunchpadGrid.SignalToGrid(index.Value),
                (SolidColorBrush)Application.Current.Styles.FindResource("ThemeAccentBrush")
            );
        }

        void Wrap_Changed(object sender, RoutedEventArgs e) {
            bool value = Wrap.IsChecked.Value;

            if (_pattern.Wrap != value) {
                bool u = _pattern.Wrap;
                bool r = value;
                List<int> path = Track.GetPath(_pattern);

                Program.Project.Undo.Add($"Pattern Wrap Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Pattern)Track.TraversePath(path)).Wrap = u;
                }, () => {
                    ((Pattern)Track.TraversePath(path)).Wrap = r;
                });

                _pattern.Wrap = value;
            }
        }

        public void SetWrap(bool value) => Wrap.IsChecked = value;

        void PlayColor(int index, Color color) {
            Dispatcher.UIThread.InvokeAsync(() => {
                Editor.SetColor(LaunchpadGrid.SignalToGrid(index), color.ToScreenBrush());
            });

            PlayExit?.Invoke(new Signal(this, _track.Launchpad, (byte)index, color.Clone()));
        }

        void FireCourier(double time) {
            Courier courier;
            PlayTimers.Add(courier = new Courier() {
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        void Tick(object sender, EventArgs e) {
            if (_pattern.Disposed || !Locked) return;

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

                    PatternFinish();
                }

                PlayTimers.Remove(courier);
            }
        }

        int PlayIndex = 0;
        object PlayLocker = new object();
        List<Courier> PlayTimers = new List<Courier>();

        void PatternStop(bool send = true) {
            lock (PlayLocker) {
                for (int i = 0; i < PlayTimers.Count; i++)
                    PlayTimers[i].Dispose();
                
                if (send && PlayIndex < _pattern.Count)
                    for (int i = 0; i < _pattern[PlayIndex].Screen.Length; i++)
                        if (_pattern[PlayIndex].Screen[i].Lit)
                            PlayColor(i, new Color(0));

                PlayTimers = new List<Courier>();
                PlayIndex = 0;
            }
        }

        void PatternPlay(object sender, RoutedEventArgs e) {
            if (Locked) {
                PatternStop();
                _pattern.MIDIEnter(new StopSignal());

                PatternFinish();
                return;
            }

            Locked = true;

            Button button = (Button)sender;

            Dispatcher.UIThread.InvokeAsync(() => {
                button.IsEnabled = true;
                button.Content = "Stop";
            });
            
            PatternStop(false);
            _pattern.MIDIEnter(new StopSignal());

            foreach (Launchpad lp in MIDI.Devices)
                if (lp.Available && lp.Type != LaunchpadType.Unknown)
                    lp.Clear();

            Editor.RenderFrame(_pattern[0]);

            for (int i = 0; i < _pattern[0].Screen.Length; i++)
                PlayExit?.Invoke(new Signal(this, _track.Launchpad, (byte)i, _pattern[0].Screen[i].Clone()));

            double time = 0;

            for (int i = 0; i < _pattern.Count; i++) {
                time += _pattern[i].Time * _pattern.Gate;
                FireCourier(time);
            }
        }

        void PatternFire(object sender, RoutedEventArgs e) {
            PlayExit = _pattern.MIDIExit;
            PatternPlay(sender, e);
        }

        void PatternFinish() {
            Dispatcher.UIThread.InvokeAsync(() => {
                Locked = false;

                Editor.RenderFrame(_pattern[_pattern.Expanded]);

                Play.Content = "Play";
                Fire.Content = "Fire";
            });

            if (_launchpad != null) PlayExit = Launchpad.Send;
            else PlayExit = null;

            MIDI.ClearState();

            for (int i = 0; i < _pattern[_pattern.Expanded].Screen.Length; i++)
                Launchpad?.Send(new Signal(this, _track.Launchpad, (byte)i, _pattern[_pattern.Expanded].Screen[i]));
        }

        static void ImportFrames(Pattern pattern, int repeats, double gate, List<Frame> frames, PlaybackType mode, bool infinite, int? root) {
            pattern.Repeats = repeats;
            pattern.Gate = gate;
            pattern.Frames = frames;
            pattern.Mode = mode;
            pattern.Infinite = infinite;
            pattern.RootKey = root;

            while (pattern.Window?.Contents.Count > 1) pattern.Window?.Contents.RemoveAt(1);
            pattern.Expanded = 0;

            for (int i = 0; i < pattern.Count; i++)
                pattern.Window?.Contents_Insert(i, pattern[i], true);

            if (pattern.Count == 1) ((FrameDisplay)pattern.Window?.Contents[1]).Remove.Opacity = 0;

            pattern.Window?.Frame_Select(0);
            pattern.Window?.Selection.Select(pattern[0]);
        }

        async void ImportFile(string filepath) {
            if (!Importer.FramesFromMIDI(filepath, out List<Frame> frames) && !Importer.FramesFromImage(filepath, out frames)) {
                await MessageWindow.Create(
                    $"An error occurred while reading the file.\n\n" +
                    "You may not have sufficient privileges to read from the destination folder, or\n" +
                    "the file you're attempting to read is invalid.",
                    null, this
                );
                
                return;
            }

            int ur = _pattern.Repeats;
            double ug = _pattern.Gate;
            List<Frame> uf = _pattern.Frames.Select(i => i.Clone()).ToList();
            PlaybackType um = _pattern.Mode;
            bool ui = _pattern.Infinite;
            int? uo = _pattern.RootKey;

            int rr = 1;
            double rg = 1;
            List<Frame> rf = frames.Select(i => i.Clone()).ToList();
            PlaybackType rm = PlaybackType.Mono;
            bool ri = false;
            int? ro = null;

            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Pattern File Imported from {Path.GetFileNameWithoutExtension(filepath)}", () => {
                ImportFrames((Pattern)Track.TraversePath(path), ur, ug, uf.Select(i => i.Clone()).ToList(), um, ui, uo);
            }, () => {
                ImportFrames((Pattern)Track.TraversePath(path), rr, rg, rf.Select(i => i.Clone()).ToList(), rm, ri, ro);
            }, () => {
                foreach (Frame frame in uf) frame.Dispose();
                foreach (Frame frame in rf) frame.Dispose();
                uf = rf = null;
            });

            ImportFrames(_pattern, 1, 1, frames, PlaybackType.Mono, false, null);
        }

        async void ImportDialog(object sender, RoutedEventArgs e) {
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
            while (source.Name != "DropZoneAfter" && source.Name != "FrameAdd") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            List<Frame> moving = ((List<ISelect>)e.Data.Get("frame")).Select(i => (Frame)i).ToList();

            Pattern source_parent = moving[0].Parent;

            int before = moving[0].IParentIndex.Value - 1;
            int after = (source.Name == "DropZoneAfter")? _pattern.Count - 1 : -1;

            bool copy = e.Modifiers.HasFlag(App.ControlInput);

            bool result = Frame.Move(moving, _pattern, after, copy);

            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == _pattern && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_parent);
                List<int> targetpath = Track.GetPath(_pattern);
                
                Program.Project.Undo.Add($"Pattern Frame {(copy? "Copied" : "Moved")}", copy
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

        bool Copyable_Insert(Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<Frame> pasted;
            try {
                pasted = paste.Contents.Cast<Frame>().ToList();
            } catch (InvalidCastException) {
                return false;
            }
            
            List<int> path = Track.GetPath(_pattern);

            undo = () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    pattern.Remove(right + i + 1);

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(pattern.Expanded);
                }
            };
            
            redo = () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = 0; i < paste.Contents.Count; i++)
                    pattern.Insert(right + i + 1, pasted[i].Clone());

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(pattern.Expanded);
                    pattern.Window.Selection.Select(pattern[right + 1], true);
                }
            };
            
            dispose = () => {
                foreach (Frame frame in pasted) frame.Dispose();
                pasted = null;
            };

            Draw = false;

            for (int i = 0; i < paste.Contents.Count; i++)
                _pattern.Insert(right + i + 1, pasted[i].Clone());

            Draw = true;

            Frame_Select(_pattern.Expanded);
            Selection.Select(_pattern[right + 1], true);
            
            return true;
        }

        bool Region_Delete(int left, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            if (_pattern.Count - (right - left + 1) == 0) return false;

            List<Frame> u = (from i in Enumerable.Range(left, right - left + 1) select _pattern[i].Clone()).ToList();

            List<int> path = Track.GetPath(_pattern);

            undo = () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = left; i <= right; i++)
                    pattern.Insert(i, u[i - left].Clone());

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(pattern.Expanded);
                }
            };
            
            redo = () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = right; i >= left; i--)
                    pattern.Remove(i);

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(pattern.Expanded);
                }
            };
            
            dispose = () => {
                foreach (Frame frame in u) frame.Dispose();
                u = null;
            };

            Draw = false;

            for (int i = right; i >= left; i--)
                _pattern.Remove(i);

            Draw = true;

            Frame_Select(_pattern.Expanded);

            return true;
        }

        public void Copy(int left, int right, bool cut = false) {
            if (Locked) return;

            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_pattern[i]);
            
            copy.StoreToClipboard();

            if (cut) Delete(left, right);
        }

        public async void Paste(int right) {
            if (Locked) return;

            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Pattern Frame Pasted", undo, redo, dispose);
        }

        public async void Replace(int left, int right) {
            if (Locked) return;

            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose)) {
                if (Region_Delete(left, right, out Action undo2, out Action redo2, out Action dispose2)) {

                    List<int> path = Track.GetPath(_pattern);

                    Program.Project.Undo.Add("Pattern Frame Replaced",
                        undo2 + undo,
                        redo + redo2 + (() => {
                            Pattern pattern = ((Pattern)Track.TraversePath(path));

                            Track.Get(pattern).Window?.Selection.Select(pattern[left + paste.Contents.Count - 1], true);
                        }),
                        dispose2 + dispose + (() => {
                            foreach (Frame frame in paste.Contents) frame.Dispose();
                            paste = null;
                        })
                    );
                    
                    Track.Get(_pattern).Window?.Selection.Select(_pattern[left + paste.Contents.Count - 1], true);
                
                } else {
                    undo.Invoke();
                    dispose.Invoke();
                }
            }
        }

        public void Duplicate(int left, int right) {
            if (Locked) return;

            List<int> path = Track.GetPath(_pattern);

            Program.Project.Undo.Add($"Pattern Frame Duplicated", () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = right - left; i >= 0; i--)
                    pattern.Remove(right + i + 1);

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(pattern.Expanded);
                }

            }, () => {
                Pattern pattern = ((Pattern)Track.TraversePath(path));

                if (pattern.Window != null) pattern.Window.Draw = false;

                for (int i = 0; i <= right - left; i++)
                    pattern.Insert(right + i + 1, pattern[left + i].Clone());

                if (pattern.Window != null) {
                    pattern.Window.Draw = true;

                    pattern.Window.Frame_Select(pattern.Expanded);
                    pattern.Window.Selection.Select(pattern[right + 1], true);
                }
            });

            Draw = false;

            for (int i = 0; i <= right - left; i++)
                _pattern.Insert(right + i + 1, _pattern[left + i].Clone());

            Draw = true;

            Frame_Select(_pattern.Expanded);
            Selection.Select(_pattern[right + 1], true);
        }

        public void Delete(int left, int right) {
            if (Locked) return;

            if (Region_Delete(left, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add($"Pattern Frame Removed", undo, redo, dispose);
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        public void Choke(int left, int right) {}
        public void Unchoke(int index) {}

        public void Mute(int left, int right) {}
        public void Rename(int left, int right) {}

        public void Export(int left, int right) {}
        public void Import(int right, string path = null) {}

        void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        void MoveWindow(object sender, PointerPressedEventArgs e) {
            if (e.ClickCount == 2) Maximize(null);
            else BeginMoveDrag(e);

            Topmost = false;
            Topmost = Preferences.AlwaysOnTop;
            Activate();
        }
        
        void Minimize() => WindowState = WindowState.Minimized;

        void Maximize(PointerEventArgs e) {
            WindowState = (WindowState == WindowState.Maximized)? WindowState.Normal : WindowState.Maximized;

            Topmost = false;
            Topmost = Preferences.AlwaysOnTop;
            Activate();
        }

        void ResizeNorthWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthWest, e);
        void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North, e);
        void ResizeNorthEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthEast, e);
        void ResizeWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West, e);
        void ResizeEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East, e);
        void ResizeSouthWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthWest, e);
        void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South, e);
        void ResizeSouthEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthEast, e);

        public static void Create(Pattern pattern, Window owner) {
            if (pattern.Window == null) {
                pattern.Window = new PatternWindow(pattern);
                
                if (owner == null || owner.WindowState == WindowState.Minimized) 
                    pattern.Window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                else
                    pattern.Window.Owner = owner;

                pattern.Window.Show();
                pattern.Window.Owner = null;
                
            } else {
                pattern.Window.WindowState = WindowState.Normal;
                pattern.Window.Activate();
            }
        }
    }
}