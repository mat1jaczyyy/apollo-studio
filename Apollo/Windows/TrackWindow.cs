using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class TrackWindow: Window {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            TitleText = this.Get<TextBlock>("Title");
            Contents = this.Get<ScrollViewer>("Contents");
        }

        Track _track;

        ScrollViewer Contents;
        Grid Root;
        TextBlock TitleText;
        
        private void UpdateTitle() => UpdateTitle(_track.ParentIndex.Value, _track.ProcessedName);
        private void UpdateTitle(int index) => UpdateTitle(index, _track.ProcessedName);
        private void UpdateTitle(string name) => UpdateTitle(_track.ParentIndex.Value, name);
        private void UpdateTitle(int index, string name)
            => Title = TitleText.Text = $"{name}{((Program.Project.FilePath != "")? $" - {Program.Project.FileName}" : "")}";

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
            chainViewer.PointerWheelChanged += Track_Scroll;

            Root = chainViewer.Get<Grid>("Layout");
            UpdateContentAlignment(Preferences.CenterTrackContents);
            Preferences.CenterTrackContentsChanged += UpdateContentAlignment;

            Contents.Content = chainViewer;

            SetEnabled();
        }

        private void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            Program.Project.PathChanged += UpdateTitle;
            _track.ParentIndexChanged += UpdateTitle;
            _track.NameChanged += UpdateTitle;
            UpdateTitle();
        }

        private void Unloaded(object sender, EventArgs e) {
            _track.Window = null;
            _track.ParentIndexChanged -= UpdateTitle;
            _track.NameChanged -= UpdateTitle;
            _track = null;

            ((ChainViewer)Contents.Content).PointerWheelChanged -= Track_Scroll;
            
            Program.Project.PathChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            Preferences.CenterTrackContentsChanged -= UpdateContentAlignment;

            Selection.Dispose();

            Program.WindowClose(this);
        }

        public virtual void SetEnabled() => Background = (IBrush)Application.Current.Styles.FindResource(_track.Enabled? "ThemeControlMidBrush" : "ThemeControlLowBrush");

        private void Track_Scroll(object sender, PointerWheelEventArgs e) => Contents.Offset = Contents.Offset.WithX(Contents.Offset.X - e.Delta.Y * 20);

        private bool InMultiPreprocess() => Selection.Start is Device &&
            Selection.Start.IParent is ISelect &&
            ((ISelect)Selection.Start.IParent).IParentIndex == null &&
            ((ISelect)Selection.Start.IParent).IParent?.GetType() == typeof(Multi);

        private async void Window_KeyDown(object sender, KeyEventArgs e) {
            if (await Program.Project.HandleKey(this, e) || Program.Project.Undo.HandleKey(e) || Selection.HandleKey(e))
                return;

            bool vertical = Selection.Start.GetType() == typeof(Chain);

            if (vertical) {
                if (e.Key == Key.Up) Selection.Move(false, e.Modifiers == InputModifiers.Shift);
                else if (e.Key == Key.Down) Selection.Move(true, e.Modifiers == InputModifiers.Shift);
                else if (e.Key == Key.Right) Selection.MoveChild();
                else if (e.Key == Key.Enter) Selection.Expand();
                else if (e.Key == Key.Left && Selection.Start.IParent.GetType() == typeof(Multi))
                    Selection.Select(((Multi)Selection.Start.IParent).Preprocess.Devices.Last());

            } else if (e.Key == Key.Left) {
                if (!(InMultiPreprocess() && Selection.Start.IParentIndex.Value == 0))
                    Selection.Move(false, e.Modifiers == InputModifiers.Shift);
            
            } else if (e.Key == Key.Right) {
                if (InMultiPreprocess() && Selection.Start.IParentIndex.Value == Selection.Start.IParent.IChildren.Count - 1)
                    Selection.Select((ISelect)((ISelect)Selection.Start.IParent).IParent, e.Modifiers == InputModifiers.Shift);

                else Selection.Move(true, e.Modifiers == InputModifiers.Shift);

            } else if (e.Key == Key.Down) Selection.MoveChild();
        }

        private void Clear() => _track.Launchpad.Clear();

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        private void Minimize() => WindowState = WindowState.Minimized;
        
        private void Expand(IPointerDevice e) {
            Point pointerRelative = e.GetPosition(this);

            PixelPoint pointerAbsolute = new PixelPoint(
                (int)(Position.X + pointerRelative.X),
                (int)(Position.Y + pointerRelative.Y)
            );

            Screen result = null;

            foreach (Screen screen in Screens.All)
                if (screen.Bounds.Contains(pointerAbsolute)) {
                    result = screen;
                    break;
                }

            if (result != null) {
                Position = new PixelPoint(result.Bounds.X, Position.Y);
                Width = result.Bounds.Width;
            }
        }

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