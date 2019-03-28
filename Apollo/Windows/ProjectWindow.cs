using System;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

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

        private Controls Contents;

        private void Contents_Insert(int index, Track track) {
            TrackViewer viewer = new TrackViewer(track);
            viewer.TrackAdded += Track_Insert;
            Contents.Insert(index + 1, viewer);
        }

        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Contents = this.Get<StackPanel>("Contents").Children;
            
            for (int i = 0; i < Program.Project.Count; i++)
                Contents.Add(new TrackViewer(Program.Project[i]));
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

        private void Track_Insert(int index) {
            Program.Project.Insert(index, new Track());
            Contents_Insert(index, Program.Project[index]);
        }

        private void Track_InsertStart() {
            Track_Insert(0);
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