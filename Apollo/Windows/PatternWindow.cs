using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

        int current;
        
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

            Editor = this.Get<LaunchpadGrid>("Editor");

            Contents = this.Get<StackPanel>("Frames").Children;

            for (int i = 0; i < _pattern.Frames.Count; i++)
                Contents_Insert(i, _pattern.Frames[i]);
            
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
            _pattern.Window = null;

            Launchpad.PatternWindow = null;
            Launchpad.Clear();
            
            Program.Project.PathChanged -= UpdateTitle;
            _track.ParentIndexChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        private void Frame_Insert(int index) {
            ((FrameDisplay)Contents[1]).Remove.Opacity = 1;

            _pattern.Frames.Insert(index, new Frame());
            Contents_Insert(index, _pattern.Frames[index]);

            if (index <= current) current++;
            Frame_Select(index);
        }

        private void Frame_InsertStart() => Frame_Insert(0);

        private void Frame_Remove(int index) {
            if (_pattern.Frames.Count == 1) return;

            if (index < current) current--;
            else if (index == current) Frame_Select(Math.Max(0, current - 1));

            Contents.RemoveAt(index + 1);
            _pattern.Frames.RemoveAt(index);

            if (_pattern.Frames.Count == 1) ((FrameDisplay)Contents[1]).Remove.Opacity = 0;
        }

        private void Frame_Select(int index) {
            ((FrameDisplay)Contents[current + 1]).Viewer.Time.FontWeight = FontWeight.Normal;

            current = index;

            ((FrameDisplay)Contents[current + 1]).Viewer.Time.FontWeight = FontWeight.Bold;

            Editor.RenderFrame(_pattern.Frames[current]);
            for (int i = 0; i < _pattern.Frames[current].Screen.Length; i++)
                Launchpad.Send(new Signal(Launchpad, (byte)i, _pattern.Frames[current].Screen[i]));
        }

        private void PadPressed(int index) {
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
            if (n.Color.Lit) Dispatcher.UIThread.InvokeAsync(() => {
                PadPressed(LaunchpadGrid.SignalToGrid(n.Index));
            });
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