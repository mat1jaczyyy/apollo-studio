using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Core;

namespace Apollo.Windows {
    public class MessageWindow: Window {
        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public TaskCompletionSource<string> Completed = new TaskCompletionSource<string>();
        
        void UpdateTopmost(bool value) => Topmost = value;

        public MessageWindow() => new InvalidOperationException();

        public MessageWindow(string message, string[] options = null) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            if (Owner == null) WindowStartupLocation = WindowStartupLocation.CenterScreen;

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

        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            foreach (Window window in App.Windows)
                if (!(window is MessageWindow))
                    window.IsVisible = false;
        }

        void Unloaded(object sender, CancelEventArgs e) {
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            
            if (App.Windows.Count(i => i is MessageWindow) <= 1)
                foreach (Window window in App.Windows)
                    if (!(window is MessageWindow))
                        window.IsVisible = true;

            this.Content = null;
        }

        void Complete(object sender, EventArgs e) {
            Completed.SetResult((string)((Button)sender).Content);
            Close();
        }

        void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        void Close(object sender, RoutedEventArgs e) => Close();

        public static async Task<string> Create(string message, string[] options, Window owner) {
            MessageWindow window = new MessageWindow(message, options) {Owner = owner};

            window.Show();
            window.Owner = null;

            return await window.Completed.Task;
        }
    }
}