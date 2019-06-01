using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Core;

namespace Apollo.Windows {
    public class ErrorWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        private void UpdateTopmost(bool value) => Topmost = value;

        public ErrorWindow(string message) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            this.Get<TextBlock>("Message").Text = message;
        }

        private void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            foreach (Window window in Application.Current.Windows)
                if (window != this)
                    window.IsVisible = false;
        }

        private void Unloaded(object sender, EventArgs e) {
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            
            foreach (Window window in Application.Current.Windows)
                if (window != this)
                    window.IsVisible = true;
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        private void Close(object sender, RoutedEventArgs e) => Close();

        public static void Create(string message, Window owner) {
            ErrorWindow window = new ErrorWindow(message) {Owner = owner};
            window.ShowDialog(owner);
            window.Owner = null;
        }
    }
}