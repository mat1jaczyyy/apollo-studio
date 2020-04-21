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

        Button Default;

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

            Default = (options?? new string[] {"OK"}).Select(i => {
                Button btn = new Button() { Content = i };
                btn.Click += Complete;
                Buttons.Children.Add(btn);
                return btn;
            }).ToList().Last();  // ToList required otherwise Last will only resolve Select on the last item
        }

        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            foreach (Window window in App.Windows)
                if (!(window is MessageWindow))
                    window.IsVisible = false;
        }

        void Unloaded(object sender, CancelEventArgs e) {
            if (!Completed.Task.IsCompleted)
                Completed.SetResult((string)Default.Content);

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

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag(e);

        void Close(object sender, RoutedEventArgs e) => Close();

        public static async Task<string> Create(string message, string[] options, Window owner) {
            MessageWindow window = new MessageWindow(message, options);
            
            if (owner == null || owner.WindowState == WindowState.Minimized)
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            else
                window.Owner = owner;

            window.Show();
            window.Owner = null;
            
            window.Topmost = true;
            window.Topmost = Preferences.AlwaysOnTop;

            return await window.Completed.Task;
        }

        public static async Task<string> CreateReadError(Window sender) => await MessageWindow.Create(
            $"An error occurred while reading the file.\n\n" +
            "You may not have sufficient privileges to read from the destination folder, or\n" +
            "the file you're attempting to read is invalid.",
            null, sender
        );

        public static async Task<string> CreateWriteError(Window sender) => await MessageWindow.Create(
            $"An error occurred while writing the file.\n\n" +
            "You may not have sufficient privileges to write to the destination folder, or\n" +
            "the file already exists but cannot be overwritten.",
            null, sender
        );
    }
}