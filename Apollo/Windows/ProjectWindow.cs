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

        private void UpdateTopmost(bool value) {
            Topmost = value;
        }

        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

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
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) {
            BeginMoveDrag();
        }
        
        private void Minimize() {
            WindowState = WindowState.Minimized;
        }

        public static void Create() {
            if (Program.Project.Window == null) {
                Program.Project.Window = new ProjectWindow();
                Program.Project.Window.Show();
            } else {
                Program.Project.Window.WindowState = WindowState.Normal;
                Program.Project.Window.Activate();
            }
        }
    }
}