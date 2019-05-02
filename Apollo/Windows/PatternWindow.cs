using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using AvaloniaColor = Avalonia.Media.Color;
using FontWeight = Avalonia.Media.FontWeight;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class PatternWindow: Window {
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
                    for (int i = 0; i < _pattern.Frames[_pattern.Expanded].Screen.Length; i++)
                        _launchpad?.Send(new Signal(Launchpad, (byte)i, _pattern.Frames[_pattern.Expanded].Screen[i]));
            }
        }

        ComboBox PortSelector;
        LaunchpadGrid Editor;
        Controls Contents;
        ColorPicker ColorPicker;
        ColorHistory ColorHistory;
        Dial Duration, Gate;
        ComboBox ComboBox;
        BoxDial Choke;
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
                PortSelector.IsEnabled = Duration.Enabled = Gate.Enabled = Import.IsEnabled = Play.IsEnabled = Fire.IsEnabled = ColorPicker.IsEnabled = ColorHistory.IsEnabled = !_locked;
            }
        }

        Action<Signal> PlayExit;
        
        private void UpdateTitle(string path, int index) => this.Get<TextBlock>("Title").Text = (path == "")
            ? $"Editing Pattern - Track {index + 1}"
            : $"Editing Pattern - Track {index + 1} - {path}";

        private void UpdateTitle(string path) => UpdateTitle(path, _track.ParentIndex.Value);
        private void UpdateTitle(int index) => UpdateTitle(Program.Project.FilePath, index);
        private void UpdateTitle() => UpdateTitle(Program.Project.FilePath, _track.ParentIndex.Value);

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

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();

            if (!ignoreExpanded && index <= _pattern.Expanded) _pattern.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (index < _pattern.Expanded) _pattern.Expanded--;
            else if (index == _pattern.Expanded) Frame_Select(Math.Max(0, _pattern.Expanded - 1));

            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();

            if (_pattern.Frames.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;
        }

        public PatternWindow(Pattern pattern) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _pattern = pattern;
            _track = Track.Get(_pattern);

            Editor = this.Get<LaunchpadGrid>("Editor");

            Duration = this.Get<Dial>("Duration");
            
            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_pattern.Gate * 100;

            ComboBox = this.Get<ComboBox>("ComboBox");
            ComboBox.SelectedItem = _pattern.Mode;

            Choke = this.Get<BoxDial>("Choke");
            if (_pattern.Choke != null) {
                Choke.Enabled = true;
                Choke.RawValue = _pattern.Choke.Value;
            }

            Import = this.Get<Button>("Import");
            Play = this.Get<Button>("Play");
            Fire = this.Get<Button>("Fire");

            Contents = this.Get<StackPanel>("Frames").Children;

            for (int i = 0; i < _pattern.Frames.Count; i++) {
                Contents_Insert(i, _pattern.Frames[i], true);
                ((FrameDisplay)Contents[i + 1]).Viewer.Time.Text = _pattern.Frames[i].TimeString;
            }
            
            if (_pattern.Frames.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;

            Launchpad = _track.Launchpad;

            PortSelector = this.Get<ComboBox>("PortSelector");
            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            ColorPicker = this.Get<ColorPicker>("ColorPicker");
            ColorHistory = this.Get<ColorHistory>("ColorHistory");

            ColorPicker.SetColor(ColorHistory.GetColor(0)?? new Color());
            ColorHistory.Select(ColorPicker.Color.Clone(), true);
            
            Frame_Select(_pattern.Expanded);
        }

        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            _track.ParentIndexChanged += UpdateTitle;
            UpdateTitle();

            ColorHistory.HistoryChanged += RenderHistory;

            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DropEvent, Drop);
        }

        private void Unloaded(object sender, EventArgs e) {
            Locked = false;

            _pattern.Window = null;

            if (Launchpad != null) {
                Launchpad.PatternWindow = null;
                Launchpad.Clear();
            }
            
            Program.Project.PathChanged -= UpdateTitle;
            _track.ParentIndexChanged -= UpdateTitle;
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
            if (Locked) return;

            Frame reference = _pattern.Frames[Math.Max(0, index - 1)];

            _pattern.Frames.Insert(index, new Frame(
                reference.Mode,
                reference.Length.Clone(),
                reference.Time
            ));

            if (Preferences.CopyPreviousFrame)
                for (int i = 0; i < _pattern.Frames[index].Screen.Length; i++)
                    _pattern.Frames[index].Screen[i] = reference.Screen[i].Clone();

            Contents_Insert(index, _pattern.Frames[index]);
            
            Frame_Select(index);
        }

        private void Frame_InsertStart() => Frame_Insert(0);

        private void Frame_Remove(int index) {
            if (Locked) return;

            if (_pattern.Frames.Count == 1) return;

            _pattern.Frames.RemoveAt(index);
            Contents_Remove(index);
        }

        private void Frame_Select(int index) {
            if (Locked) return;

            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.FontWeight = FontWeight.Normal;

            _pattern.Expanded = index;

            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.FontWeight = FontWeight.Bold;
            
            Duration.UsingSteps = _pattern.Frames[_pattern.Expanded].Mode;
            Duration.Length = _pattern.Frames[_pattern.Expanded].Length;
            Duration.RawValue = _pattern.Frames[_pattern.Expanded].Time;

            Editor.RenderFrame(_pattern.Frames[_pattern.Expanded]);

            for (int i = 0; i < _pattern.Frames[_pattern.Expanded].Screen.Length; i++)
                Launchpad?.Send(new Signal(Launchpad, (byte)i, _pattern.Frames[_pattern.Expanded].Screen[i]));
        }

        private void ColorPicker_Changed(Color color) => ColorHistory.Select(color.Clone());

        private void ColorHistory_Changed(Color color) {
            ColorPicker.SetColor(color.Clone());
            RenderHistory();
        }

        private void RenderHistory() {
            if (!historyShowing) return;
            ColorHistory.Render(Launchpad);
        }

        Color drawingState;
        
        private void PadStarted(int index) => drawingState = (_pattern.Frames[_pattern.Expanded].Screen[LaunchpadGrid.GridToSignal(index)] == ColorPicker.Color)
            ? new Color(0)
            : ColorPicker.Color;
    
        private void PadPressed(int index, InputModifiers mods = InputModifiers.None) {
            if (Locked) return;

            int signalIndex = LaunchpadGrid.GridToSignal(index);

            if (mods.HasFlag(InputModifiers.Control)) {
                if (_pattern.Frames[_pattern.Expanded].Screen[signalIndex] != new Color(0)) {
                    Color color = _pattern.Frames[_pattern.Expanded].Screen[signalIndex];
                    ColorPicker.SetColor(color.Clone());
                    ColorHistory.Select(color.Clone(), true);
                }
                return;
            }

            if (_pattern.Frames[_pattern.Expanded].Screen[signalIndex] != ColorPicker.Color) Dispatcher.UIThread.InvokeAsync(() => {
                ColorHistory.Use();
            });

            _pattern.Frames[_pattern.Expanded].Screen[signalIndex] = drawingState.Clone();

            SolidColorBrush brush = (SolidColorBrush)_pattern.Frames[_pattern.Expanded].Screen[signalIndex].ToBrush();

            Editor.SetColor(index, brush);
            ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Launchpad.SetColor(index, brush);

            Launchpad?.Send(new Signal(Launchpad, (byte)signalIndex, _pattern.Frames[_pattern.Expanded].Screen[signalIndex]));
        }

        private void HandleGesture(int x, int y) {
            if (x == -1 && y == 0) { // Left
                if (_pattern.Expanded == 0) Frame_Insert(0);
                else Frame_Select(_pattern.Expanded - 1);

            } else if (x == 1 && y == 0) { // Right
                if (_pattern.Expanded == _pattern.Frames.Count - 1) Frame_Insert(_pattern.Frames.Count);
                else Frame_Select(_pattern.Expanded + 1);

            } else if (x == 0 && y == 1) { // Up
                origin = -1;
                historyShowing = Locked = true;
                RenderHistory();
                
            } else if (x == 0 && y == -1) { // Down
                PadStarted(-1);
                PadPressed(-1);
                
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

        private void KeyReleased(object sender, KeyEventArgs e) {
            if (e.Key == Key.NumPad4) HandleGesture(-1, 0);
            else if (e.Key == Key.NumPad6) HandleGesture(1, 0);
            else if (e.Key == Key.NumPad8) HandleGesture(0, 1);
            else if (e.Key == Key.NumPad2) HandleGesture(0, -1);
            else if (e.Key == Key.NumPad7) HandleGesture(-1, 1);
            else if (e.Key == Key.NumPad3) HandleGesture(1, -1);
            else if (e.Key == Key.NumPad9) HandleGesture(1, 1);
            else if (e.Key == Key.NumPad1) HandleGesture(-1, -1);
        }

        private void Duration_Changed(double value, InputModifiers mods) {
            if (mods.HasFlag(InputModifiers.Control)) {
                for (int i = 0; i < _pattern.Frames.Count; i++) {
                    _pattern.Frames[i].Mode = false;
                    _pattern.Frames[i].Time = (int)value;
                    ((FrameDisplay)Contents[i + 1]).Viewer.Time.Text = _pattern.Frames[i].TimeString;
                }
            } else {
                _pattern.Frames[_pattern.Expanded].Time = (int)value;
                ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.Text = _pattern.Frames[_pattern.Expanded].TimeString;
            }
        }

        private void Duration_StepChanged(int value, InputModifiers mods) {
            if (mods.HasFlag(InputModifiers.Control))
                for (int i = 0; i < _pattern.Frames.Count; i++) {
                    _pattern.Frames[i].Mode = true;
                    _pattern.Frames[i].Length.Step = value;
                    ((FrameDisplay)Contents[i + 1]).Viewer.Time.Text = _pattern.Frames[_pattern.Expanded].TimeString;
                } 
            else
                ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.Text = _pattern.Frames[_pattern.Expanded].TimeString;
        }

        private void Duration_ModeChanged(bool value, InputModifiers mods) {
            if (mods.HasFlag(InputModifiers.Control)) {
                for (int i = 0; i < _pattern.Frames.Count; i++) {
                    _pattern.Frames[i].Mode = value;
                    ((FrameDisplay)Contents[i + 1]).Viewer.Time.Text = _pattern.Frames[i].TimeString;
                }
            } else {
                _pattern.Frames[_pattern.Expanded].Mode = value;
                ((FrameDisplay)Contents[_pattern.Expanded + 1]).Viewer.Time.Text = _pattern.Frames[_pattern.Expanded].TimeString;
            }
        }

        private void Gate_Changed(double value) => _pattern.Gate = (decimal)(value / 100);

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) => _pattern.Mode = (string)ComboBox.SelectedItem;

        private void Choke_MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right && (Choke.Enabled = !Choke.Enabled) == false)
                _pattern.Choke = null;
        }

        private void Choke_Changed(double value) => _pattern.Choke = (int)value;

        private void FireCourier(Color color, byte index, decimal time) {
            Courier courier = new Courier() {
                Info = new Signal(_track.Launchpad, (byte)index, color.Clone()),
                AutoReset = false,
                Interval = (double)time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void FireStopCourier(decimal time) {
            Courier courier = new Courier() {
                AutoReset = false,
                Interval = (double)time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            if (!Locked) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            if (courier.Info == null) {
                for (int i = 0; i < _pattern.Frames.Last().Screen.Length; i++)
                    if (_pattern.Frames.Last().Screen[i].Lit)
                        PlayExit?.Invoke(new Signal(_track.Launchpad, (byte)i, new Color(0)));

                if (_launchpad != null) PlayExit = Launchpad.Send;
                else PlayExit = null;

                Dispatcher.UIThread.InvokeAsync(() => {
                    Locked = false;

                    Editor.RenderFrame(_pattern.Frames[_pattern.Expanded]);
                });

                for (int i = 0; i < _pattern.Frames[_pattern.Expanded].Screen.Length; i++)
                    Launchpad?.Send(new Signal(_track.Launchpad, (byte)i, _pattern.Frames[_pattern.Expanded].Screen[i]));

            } else {
                Signal n = (Signal)courier.Info;
                
                Dispatcher.UIThread.InvokeAsync(() => {
                    Editor.SetColor(LaunchpadGrid.SignalToGrid(n.Index), (SolidColorBrush)n.Color.ToBrush());
                });

                PlayExit?.Invoke(n.Clone());
            }
        }

        private void PatternPlay(object sender, RoutedEventArgs e) {
            if (Locked) return;
            Locked = true;

            Editor.RenderFrame(_pattern.Frames[0]);

            for (int i = 0; i < _pattern.Frames[0].Screen.Length; i++)
                PlayExit?.Invoke(new Signal(_track.Launchpad, (byte)i, _pattern.Frames[0].Screen[i].Clone()));
            
            decimal time = (_pattern.Frames[0].Mode? (int)_pattern.Frames[0].Length : _pattern.Frames[0].Time) * _pattern.Gate;

            for (int i = 1; i < _pattern.Frames.Count; i++) {
                for (int j = 0; j < _pattern.Frames[i].Screen.Length; j++)
                    if (_pattern.Frames[i].Screen[j] != _pattern.Frames[i - 1].Screen[j])
                        FireCourier(_pattern.Frames[i].Screen[j], (byte)j, time);

                time += (_pattern.Frames[i].Mode? (int)_pattern.Frames[i].Length : _pattern.Frames[i].Time) * _pattern.Gate;
            }
            
            FireStopCourier(time);
        }

        private void PatternFire(object sender, RoutedEventArgs e) {
            if (Locked) return;

            PlayExit = _pattern.MIDIExit;
            PatternPlay(sender, e);
        }

        private void ImportFile(string path) {
            if (!Importer.FramesFromMIDI(path, out List<Frame> frames) &&
                !Importer.FramesFromImage(path, out frames))
                return;

            _pattern.Frames = frames;
            _pattern.Gate = 1;

            Gate.RawValue = (double)_pattern.Gate * 100;

            while (Contents.Count > 1) Contents.RemoveAt(1);
            _pattern.Expanded = 0;

            for (int i = 0; i < _pattern.Frames.Count; i++) {
                Contents_Insert(i, _pattern.Frames[i], true);
                ((FrameDisplay)Contents[i + 1]).Viewer.Time.Text = _pattern.Frames[i].TimeString;
            }

            if (_pattern.Frames.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;

            Frame_Select(0);
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

        private void DragOver(object sender, DragEventArgs e) {
            e.DragEffects &= DragDropEffects.Copy;

            if (!e.Data.Contains(DataFormats.FileNames))
                e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e) {
            if (e.Data.Contains(DataFormats.FileNames)) {
                List<string> filenames = e.Data.GetFileNames().ToList();
                if (filenames.Count == 1) ImportFile(filenames[0]);
            }
        }

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