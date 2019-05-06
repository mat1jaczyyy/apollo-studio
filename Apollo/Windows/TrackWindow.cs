using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class TrackWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Track _track;
        Grid Root;
        
        private void UpdateTitle(string path, int index) => this.Get<TextBlock>("Title").Text = (path == "")
            ? $"Track {index + 1}"
            : $"Track {index + 1} - {path}";

        private void UpdateTitle(string path) => UpdateTitle(path, _track.ParentIndex.Value);
        private void UpdateTitle(int index) => UpdateTitle(Program.Project.FilePath, index);
        private void UpdateTitle() => UpdateTitle(Program.Project.FilePath, _track.ParentIndex.Value);

        private void UpdateTopmost(bool value) => Topmost = value;

        private void UpdateContentAlignment(bool value) => Root.ColumnDefinitions[0] = new ColumnDefinition(1, value? GridUnitType.Star : GridUnitType.Auto);

        public SelectionManager Selection = new SelectionManager();

        public TrackWindow(Track track) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _track = track;

            ChainViewer chainViewer = new ChainViewer(_track.Chain);

            Root = chainViewer.Get<Grid>("Layout");
            UpdateContentAlignment(Preferences.CenterTrackContents);
            Preferences.CenterTrackContentsChanged += UpdateContentAlignment;

            this.Get<ScrollViewer>("Contents").Content = chainViewer;
        }

        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            _track.ParentIndexChanged += UpdateTitle;
            UpdateTitle();
        }

        private void Unloaded(object sender, EventArgs e) {
            _track.Window = null;
            
            Program.Project.PathChanged -= UpdateTitle;
            _track.ParentIndexChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            Preferences.CenterTrackContentsChanged -= UpdateContentAlignment;

            Program.WindowClose(this);
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void ResizeWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West);

        private void ResizeEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East);

        public static void Create(Track track, Window owner) {
            if (track.Window == null) {
                track.Window = new TrackWindow(track) {Owner = owner};
                track.Window.Show();
                track.Window.Owner = null;
            } else {
                track.Window.WindowState = WindowState.Normal;
                track.Window.Activate();
            }
        }
    }
}