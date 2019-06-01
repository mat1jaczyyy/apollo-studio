using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Visuals;

using Apollo.Core;
using UndoEntry = Apollo.Helpers.UndoManager.UndoEntry;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class UndoWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTopmost(bool value) => Topmost = value;

        ScrollViewer ScrollViewer;
        StackPanel Contents;

        int saved = 0;
        int current = 0;

        public void Contents_Insert(int index, UndoEntry entry) {
            UndoEntryInfo viewer = new UndoEntryInfo(entry);

            viewer.Selected += UndoEntry_Select;

            Contents.Children.Insert(index, viewer);
            
            Dispatcher.UIThread.Post(() => ScrollViewer.Offset = ScrollViewer.Offset.WithY(Contents.Bounds.Height), DispatcherPriority.Background);
        }

        public void Contents_Remove(int index) => Contents.Children.RemoveAt(index);
        
        public UndoWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            ScrollViewer = this.Get<ScrollViewer>("ScrollViewer");

            Contents = this.Get<StackPanel>("Contents");

            for (int i = 0; i < Program.Project.Undo.History.Count; i++)
                Contents_Insert(i, Program.Project.Undo.History[i]);
            
            Program.Project.Undo.SavedPositionChanged += HighlightSaved;

            HighlightPosition(Program.Project.Undo.Position);
        }

        private void Loaded(object sender, EventArgs e) => Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

        private void Unloaded(object sender, EventArgs e) {
            Program.Project.Undo.Window = null;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.Project.Undo.SavedPositionChanged -= HighlightSaved;
        }

        private void UndoEntry_Select(int index) => Program.Project.Undo.Select(index);

        public void HighlightSaved(int index) {
            if (saved != current)
                ((UndoEntryInfo)(Contents.Children[saved])).Background = SolidColorBrush.Parse("Transparent");

            if (index != current)
                ((UndoEntryInfo)(Contents.Children[saved = index])).Background = (SolidColorBrush)Application.Current.Styles.FindResource("ThemeControlHigherBrush");
        }

        public void HighlightPosition(int index) {
            ((UndoEntryInfo)(Contents.Children[current])).Background = SolidColorBrush.Parse("Transparent");
            ((UndoEntryInfo)(Contents.Children[current = index])).Background = (SolidColorBrush)Application.Current.Styles.FindResource("ThemeAccentBrush2");
        
            if (Program.Project.Undo.SavedPosition.HasValue)
                HighlightSaved(Program.Project.Undo.SavedPosition.Value);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (Program.Project.HandleKey(this, e) || Program.Project.Undo.HandleKey(e)) {
                this.Focus();
                return;
            }

            if (e.Key == Key.Up) Program.Project.Undo.Undo();
            else if (e.Key == Key.Down) Program.Project.Undo.Redo();
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North);

        private void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South);

        public static void Create(Window owner) {
            if (Program.Project.Undo.Window == null) {
                Program.Project.Undo.Window = new UndoWindow() {Owner = owner};
                Program.Project.Undo.Window.Show();
                Program.Project.Undo.Window.Owner = null;
            } else {
                Program.Project.Undo.Window.WindowState = WindowState.Normal;
                Program.Project.Undo.Window.Activate();
            }
        }
    }
}