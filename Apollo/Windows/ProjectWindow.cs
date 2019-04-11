using System;
using System.Globalization;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTitle(string path) => this.Get<TextBlock>("Title").Text = (path == "")? "New Project" : path;

        private void UpdatePage() => Page.RawValue = Program.Project.Page;
        private void HandlePage() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePage);

        private void UpdateTopmost(bool value) => Topmost = value;

        Controls Contents;
        TextBox BPM;
        HorizontalDial Page;

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
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Contents = this.Get<StackPanel>("Contents").Children;
            
            if (Program.Project.Count == 0) this.Get<TrackAdd>("TrackAdd").AlwaysShowing = true;
            
            for (int i = 0; i < Program.Project.Count; i++)
                Contents_Insert(i, Program.Project[i]);
            
            BPM = this.Get<TextBox>("BPM");
            BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture);

            BPM.GetObservable(TextBox.TextProperty).Subscribe(BPM_Changed);

            Page = this.Get<HorizontalDial>("Page");
            Page.RawValue = Program.Project.Page;
        }
        
        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            UpdateTitle(Program.Project.FilePath);

            Program.Project.PageChanged += HandlePage;
            UpdatePage();
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
            if (text == "") text = "0";

            Action update = () => { BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture); };

            if (Decimal.TryParse(text, out Decimal value))
                if (20 <= value && value <= 999) {
                    Program.Project.BPM = value;
                    update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    update = () => {
                        if (value <= 0) BPM.Text = "0";
                        if (value > 999) BPM.Text = "999";
                        BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush");
                    };
                }

            Dispatcher.UIThread.InvokeAsync(update);
        }
        
        private void BPM_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) this.Focus();
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