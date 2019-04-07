using System;
using System.Globalization;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTitle(string path) => this.Get<TextBlock>("Title").Text = (path == "")? "New Project" : path;

        private void UpdateTopmost(bool value) => Topmost = value;

        Controls Contents;
        TextBox BPM;

        private void Contents_Insert(int index, Track track) {
            TrackInfo viewer = new TrackInfo(track);
            viewer.TrackAdded += Track_Insert;
            viewer.TrackRemoved += Track_Remove;
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
            
            if (Program.Project.Count == 0) this.Get<TrackAdd>("TrackAdd").AlwaysShowing = true;
            
            for (int i = 0; i < Program.Project.Count; i++)
                Contents_Insert(i, Program.Project[i]);
            
            BPM = this.Get<TextBox>("BPM");
            BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture);

            BPM.GetObservable(TextBox.TextProperty).Subscribe(BPM_Changed);

            this.Get<HorizontalDial>("Page").RawValue = Program.Project.Page;
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
            this.Get<TrackAdd>("TrackAdd").AlwaysShowing = false;
        }

        private void Track_InsertStart() => Track_Insert(0);

        private void Track_Remove(int index) {
            Contents.RemoveAt(index + 1);
            Program.Project[index].Window?.Close();
            Program.Project.Remove(index);

            if (Program.Project.Count == 0) this.Get<TrackAdd>("TrackAdd").AlwaysShowing = true;
        }

        private void Page_Changed(double value) => Program.Project.Page = (int)value;

        private void BPM_Changed(string text) {
            if (text == null) return;

            if (Decimal.TryParse(text, out Decimal value)) Program.Project.BPM = value;
            else BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture);
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North);

        private void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South);

        public static void Create(Window owner) {
            if (Program.Project.Window == null) {
                Program.Project.Window = new ProjectWindow() {Owner = owner};
                Program.Project.Window.Show();
                Program.Project.Window.Owner = null;
            } else {
                Program.Project.Window.WindowState = WindowState.Normal;
                Program.Project.Window.Activate();
            }
        }
    }
}