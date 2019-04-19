using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

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

        Controls Contents;
        HorizontalAdd FrameAdd;
        
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

            Contents = this.Get<StackPanel>("Frames").Children;
            FrameAdd = this.Get<HorizontalAdd>("FrameAdd");

            if (_pattern.Frames.Count == 0) FrameAdd.AlwaysShowing = true;

            for (int i = 0; i < _pattern.Frames.Count; i++)
                Contents_Insert(i, _pattern.Frames[i]);
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
            _pattern.Frames.Insert(index, new Frame());
            Contents_Insert(index, _pattern.Frames[index]);
            FrameAdd.AlwaysShowing = false;
        }

        private void Frame_InsertStart() => Frame_Insert(0);

        private void Frame_Remove(int index) {
            Contents.RemoveAt(index + 1);
            _pattern.Frames.RemoveAt(index);

            if (_pattern.Frames.Count == 0) FrameAdd.AlwaysShowing = true;
        }

        public void MIDIEnter(Signal n) {
            
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