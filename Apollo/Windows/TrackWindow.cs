using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class TrackWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Track _track;
        
        private void UpdateTitle(string path) {
            this.Get<TextBlock>("Title").Text = (path == "")
                ? $"Track {_track.ParentIndex + 1}"
                : $"Track {_track.ParentIndex + 1} - {path}";
        }

        public TrackWindow(Track track) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));

            _track = track;
            _track.Window = this;

            this.Get<ScrollViewer>("Contents").Content = new ChainViewer(_track.Chain);
        }

        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            UpdateTitle(Program.Project.FilePath);
        }

        private void Unloaded(object sender, EventArgs e) {
            _track.Window = null;

            Program.Project.PathChanged -= UpdateTitle;
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) {
            BeginMoveDrag();
        }

        private void Minimize() {
            WindowState = WindowState.Minimized;
        }
    }
}