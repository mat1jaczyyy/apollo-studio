using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Windows {
    public class UpdateWindow: Window {
        static Image UpdateImage = (Image)Application.Current.Styles.FindResource("UpdateImage");

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
        }

        Grid Root;

        public UpdateWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Root.Children.Add(UpdateImage);
        }

        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));
        }
        
        void Unloaded(object sender, EventArgs e) {
            Root.Children.Remove(UpdateImage);

            this.Content = null;
        }

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        public static void Create(Window owner) {
            UpdateWindow window = new UpdateWindow() {Owner = owner};
            window.Show();
            window.Owner = null;
        }
    }
}
