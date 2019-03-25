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
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTitle(string path) {
            this.Get<TextBlock>("Title").Text = (path == "")? "New Project" : path;
        }

        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));

            Program.Project.Window = this;

            Controls Contents = this.Get<StackPanel>("Contents").Children;
            
            foreach (Track track in Program.Project.Tracks)
                Contents.Add(new TrackViewer(track));
        }
        
        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            UpdateTitle(Program.Project.FilePath);
        }

        private void Unloaded(object sender, EventArgs e) {
            Program.Project.Window = null;

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