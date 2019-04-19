using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            get => (IBrush)this.Resources["Color"];
            set => this.Resources["Color"] = value;
        }

        public FadeThumb() {
            InitializeComponent();
            
            Base = this.Get<Thumb>("Thumb");
            Base.AddHandler(InputElement.PointerPressedEvent, MouseDown, RoutingStrategies.Tunnel);
            Base.AddHandler(InputElement.PointerReleasedEvent, MouseUp, RoutingStrategies.Tunnel);
        }

        bool dragged = false;

        private void DragStarted(object sender, VectorEventArgs e) => dragged = false;

        private void DragCompleted(object sender, VectorEventArgs e) {
            if (!dragged) Focused?.Invoke(this);
        }

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton != MouseButton.Left) e.Handled = true;
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) {
                Deleted?.Invoke(this);
                e.Handled = true;
            }
        }

        private void MouseMove(object sender, VectorEventArgs e) {
            dragged = true;
            Moved?.Invoke(this, e);
        }
        
        public void Select() => this.Resources["Outline"] = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => this.Resources["Outline"] = new SolidColorBrush(new Color(0, 255, 255, 255));
    }
}
