using System;
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
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class PatternWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Pattern _pattern;
        Track _track;
        Launchpad Launchpad;

        LaunchpadGrid Editor;
        Controls Contents;
        ColorPicker ColorPicker;
        Dial Duration, Gate;
        Button Play, Fire;

        int current;

        bool _playing = false;
        bool Playing {
            get => _playing;
            set {
                _playing = value;
                Duration.Enabled = Gate.Enabled = Play.IsEnabled = Fire.IsEnabled = !_playing;
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

        private void Contents_Insert(int index, Frame frame) {
            FrameDisplay viewer = new FrameDisplay(frame, _pattern);
            viewer.FrameAdded += Frame_Insert;
            viewer.FrameRemoved += Frame_Remove;
            viewer.FrameSelected += Frame_Select;
            Contents.Insert(index + 1, viewer);
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

            Launchpad = _track.Launchpad;
            Launchpad.PatternWindow?.Close();
            Launchpad.PatternWindow = this;
            Launchpad.Clear();

            PlayExit = Launchpad.Send;

            Editor = this.Get<LaunchpadGrid>("Editor");

            Duration = this.Get<Dial>("Duration");
            
            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_pattern.Gate * 100;

            Play = this.Get<Button>("Play");
            Fire = this.Get<Button>("Fire");

            Contents = this.Get<StackPanel>("Frames").Children;

            for (int i = 0; i < _pattern.Frames.Count; i++) {
                Contents_Insert(i, _pattern.Frames[i]);
                ((FrameDisplay)Contents[i + 1]).Viewer.Time.Text = _pattern.Frames[i].TimeString;
            }
            
            if (_pattern.Frames.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;
            
            Frame_Select(0);

            ColorPicker = this.Get<ColorPicker>("ColorPicker");
        }

        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            _track.ParentIndexChanged += UpdateTitle;
            UpdateTitle();
        }

        private void Unloaded(object sender, EventArgs e) {
            Playing = false;

            _pattern.Window = null;

            Launchpad.PatternWindow = null;
            Launchpad.Clear();
            
            Program.Project.PathChanged -= UpdateTitle;
            _track.ParentIndexChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        private void Frame_Insert(int index) {
            if (Playing) return;

            ((FrameDisplay)Contents[1]).Remove.Opacity = 1;

            Frame reference = _pattern.Frames[Math.Max(0, index - 1)];

            _pattern.Frames.Insert(index, new Frame(
                reference.Mode,
                reference.Length,
                reference.Time
            ));

            Contents_Insert(index, _pattern.Frames[index]);

            if (index <= current) current++;
            Frame_Select(index);
        }

        private void Frame_InsertStart() => Frame_Insert(0);

        private void Frame_Remove(int index) {
            if (Playing) return;

            if (_pattern.Frames.Count == 1) return;

            if (index < current) current--;
            else if (index == current) Frame_Select(Math.Max(0, current - 1));

            Contents.RemoveAt(index + 1);
            _pattern.Frames.RemoveAt(index);

            if (_pattern.Frames.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;
        }

        private void Frame_Select(int index) {
            if (Playing) return;

            ((FrameDisplay)Contents[current + 1]).Viewer.Time.FontWeight = FontWeight.Normal;

            current = index;

            ((FrameDisplay)Contents[current + 1]).Viewer.Time.FontWeight = FontWeight.Bold;
            
            Duration.UsingSteps = _pattern.Frames[current].Mode;
            Duration.Length = _pattern.Frames[current].Length;
            Duration.RawValue = _pattern.Frames[current].Time;

            Editor.RenderFrame(_pattern.Frames[current]);

            for (int i = 0; i < _pattern.Frames[current].Screen.Length; i++)
                Launchpad.Send(new Signal(Launchpad, (byte)i, _pattern.Frames[current].Screen[i]));
        }

        private void PadPressed(int index) {
            if (Playing) return;

            int signalIndex = LaunchpadGrid.GridToSignal(index);

            _pattern.Frames[current].Screen[signalIndex] = (_pattern.Frames[current].Screen[signalIndex] == ColorPicker.Color)
                ? new Color(0)
                : ColorPicker.Color.Clone();

            SolidColorBrush brush = (SolidColorBrush)_pattern.Frames[current].Screen[signalIndex].ToBrush();

            Editor.SetColor(index, brush);
            ((FrameDisplay)Contents[current + 1]).Viewer.Launchpad.SetColor(index, brush);

            Launchpad.Send(new Signal(Launchpad, (byte)signalIndex, _pattern.Frames[current].Screen[signalIndex]));
        }

        public void MIDIEnter(Signal n) {
            if (Playing) return;

            if (n.Color.Lit) Dispatcher.UIThread.InvokeAsync(() => {
                PadPressed(LaunchpadGrid.SignalToGrid(n.Index));
            });
        }

        private void Duration_Changed(double value) {
            _pattern.Frames[current].Time = (int)value;
            ((FrameDisplay)Contents[current + 1]).Viewer.Time.Text = _pattern.Frames[current].TimeString;
        }

        private void Duration_StepChanged(int value) => ((FrameDisplay)Contents[current + 1]).Viewer.Time.Text = _pattern.Frames[current].TimeString;

        private void Duration_ModeChanged(bool value) {
            _pattern.Frames[current].Mode = value;
            ((FrameDisplay)Contents[current + 1]).Viewer.Time.Text = _pattern.Frames[current].TimeString;
        }

        private void Gate_Changed(double value) => _pattern.Gate = (decimal)(value / 100);

        private void FireCourier(Color color, byte index, int time) {
            Courier courier = new Courier() {
                Info = new Signal(Launchpad, (byte)index, color),
                AutoReset = false,
                Interval = time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void FireStopCourier(int time) {
            Courier courier = new Courier() {
                AutoReset = false,
                Interval = time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            if (!Playing) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            if (courier.Info == null) {
                PlayExit = Launchpad.Send;

                Dispatcher.UIThread.InvokeAsync(() => {
                    Playing = false;

                    Editor.RenderFrame(_pattern.Frames[current]);
                });

                for (int i = 0; i < _pattern.Frames[current].Screen.Length; i++)
                    Launchpad.Send(new Signal(Launchpad, (byte)i, _pattern.Frames[current].Screen[i]));

            } else {
                Signal n = (Signal)courier.Info;
                
                Dispatcher.UIThread.InvokeAsync(() => {
                    Editor.SetColor(LaunchpadGrid.SignalToGrid(n.Index), (SolidColorBrush)n.Color.ToBrush());
                });

                PlayExit?.Invoke(n);
            }
        }

        private void PatternPlay(object sender, RoutedEventArgs e) {
            if (Playing) return;
            Playing = true;

            Editor.RenderFrame(_pattern.Frames[0]);

            for (int i = 0; i < _pattern.Frames[0].Screen.Length; i++)
                PlayExit?.Invoke(new Signal(Launchpad, (byte)i, _pattern.Frames[0].Screen[i]));
            
            decimal time = (_pattern.Frames[0].Mode? (int)_pattern.Frames[0].Length : _pattern.Frames[0].Time) * _pattern.Gate;

            for (int i = 1; i < _pattern.Frames.Count; i++) {
                for (int j = 0; j < _pattern.Frames[i].Screen.Length; j++)
                    if (_pattern.Frames[i].Screen[j] != _pattern.Frames[i - 1].Screen[j])
                        FireCourier(_pattern.Frames[i].Screen[j].Clone(), (byte)j, (int)time);

                time += (_pattern.Frames[i].Mode? (int)_pattern.Frames[i].Length : _pattern.Frames[i].Time) * _pattern.Gate;
            }
            
            FireStopCourier((int)time);
        }

        private void PatternFire(object sender, RoutedEventArgs e) {
            PlayExit = _pattern.MIDIExit;
            PatternPlay(sender, e);
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        public static void Create(Pattern pattern, Window owner) {
            if (pattern.Window == null) {
                pattern.Window = new PatternWindow(pattern) {Owner = owner};
                pattern.Window.ShowDialog(owner);
                pattern.Window.Owner = null;
            } else {
                pattern.Window.WindowState = WindowState.Normal;
                pattern.Window.Activate();
            }
        }
    }
}