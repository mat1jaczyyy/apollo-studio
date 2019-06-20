using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Core;

namespace Apollo.Windows {
    public class MessageWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public TaskCompletionSource<string> Completed = new TaskCompletionSource<string>();
        
        private void UpdateTopmost(bool value) => Topmost = value;

        public MessageWindow(string message, string[] options = null) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            this.Get<TextBlock>("Message").Text = message;

            StackPanel Buttons = this.Get<StackPanel>("Buttons");

            foreach (string option in options?? new string[] {"OK"}) {
                Button button = new Button() { Content = option };
                button.Click += Complete;
                Buttons.Children.Add(button);
            }
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

        private void Complete(object sender, EventArgs e) {
            Completed.SetResult((string)((Button)sender).Content);
            Close();
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        private void Close(object sender, RoutedEventArgs e) => Close();

        public static async Task<string> Create(string message, string[] options, Window owner) {
            MessageWindow window = new MessageWindow(message, options) {Owner = owner};
            if (owner == null) window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.Show();
            window.Owner = null;

            return await window.Completed.Task;
        }
    }
}