using System;
using System.Globalization;

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
        TrackAdd TrackAdd;

        TextBox BPM;
        HorizontalDial Page;

        public void Contents_Insert(int index, Track track) {
            TrackInfo viewer = new TrackInfo(track);
            viewer.TrackAdded += Track_Insert;
            viewer.TrackRemoved += Track_Remove;

            Contents.Insert(index + 1, viewer);
            TrackAdd.AlwaysShowing = false;
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            if (Contents.Count == 1) TrackAdd.AlwaysShowing = true;
        }
        
        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Contents = this.Get<StackPanel>("Contents").Children;
            TrackAdd = this.Get<TrackAdd>("TrackAdd");
            
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
        }

        private void Track_InsertStart() => Track_Insert(0);

        private void Track_Remove(int index) {
            Contents_Remove(index);
            Program.Project[index].Window?.Close();
            Program.Project.Remove(index);
        }

        private void Page_Changed(double value) => Program.Project.Page = (int)value;

        private Action BPM_Update;

        private void BPM_Changed(string text) {
            if (text == null) return;
            if (text == "") text = "0";

            BPM_Update = () => { BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture); };

            if (int.TryParse(text, out int value)) {
                if (20 <= value && value <= 999) {
                    Program.Project.BPM = value;
                    BPM_Update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    BPM_Update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                BPM_Update += () => { 
                    if (value <= 0) text = "0";
                    else text = text.TrimStart('0');

                    if (value > 999) text = "999";
                    
                    BPM.Text = text;
                };
            }

            Dispatcher.UIThread.InvokeAsync(() => {
                BPM_Update?.Invoke();
                BPM_Update = null;
            });
        }
        
        private void BPM_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) this.Focus();
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void CheckClose(bool force) {
            if (force) foreach (Track track in Program.Project.Tracks) track.Window?.Close();
            Close();
        }

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