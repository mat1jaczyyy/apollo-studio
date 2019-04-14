using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class FadeThumb: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void MovedEventHandler(FadeThumb sender, VectorEventArgs e);
        public event MovedEventHandler Moved;

        public delegate void FadeThumbEventHandler(FadeThumb sender);
        public event FadeThumbEventHandler Focused;
        public event FadeThumbEventHandler Deleted;

        public Thumb Base;

        public FadeThumb() {
            InitializeComponent();
            
            Base = this.Get<Thumb>("Thumb");
        }

        private void MouseDown(object sender, VectorEventArgs e) => Focused?.Invoke(this);

        private void MouseMove(object sender, VectorEventArgs e) => Moved?.Invoke(this, e);

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) Deleted?.Invoke(this);
        }
    }
}
