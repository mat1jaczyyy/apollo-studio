using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Update.Windows {
    public class MainWindow: Window {
        static Image SplashImage = (Image)Application.Current.Styles.FindResource("SplashImage");

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
        }

        Grid Root;

        public MainWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Root.Children.Add(SplashImage);
        }

        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));
        }
        
        void Unloaded(object sender, EventArgs e) {
            Root.Children.Remove(SplashImage);

            this.Content = null;
        }

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
    }
}
