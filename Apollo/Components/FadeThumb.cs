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

        public IBrush Fill {
            get => Base.Background;
            set => Base.Background = value;
        }

        public FadeThumb() {
            InitializeComponent();
            
            Base = this.Get<Thumb>("Thumb");
        }

        bool dragged = false;

        private void DragStarted(object sender, VectorEventArgs e) => dragged = false;

        private void DragCompleted(object sender, VectorEventArgs e) {
            if (!dragged) Focused?.Invoke(this);
        }

        private void MouseMove(object sender, VectorEventArgs e) {
            dragged = true;
            Moved?.Invoke(this, e);
        } 

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) Deleted?.Invoke(this);
        }

        public void Select() => Base.Foreground = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => Base.Foreground = new SolidColorBrush(new Color(0, 255, 255, 255));
    }
}
