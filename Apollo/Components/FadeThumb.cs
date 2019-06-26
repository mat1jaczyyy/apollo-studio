using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class FadeThumb: UserControl {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Base = this.Get<Thumb>("Thumb");
        }

        public delegate void MovedEventHandler(FadeThumb sender, double change, double? total);
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
            
            Base.AddHandler(InputElement.PointerPressedEvent, MouseDown, RoutingStrategies.Tunnel);
            Base.AddHandler(InputElement.PointerReleasedEvent, MouseUp, RoutingStrategies.Tunnel);
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Moved = null;
            Focused = null;
            Deleted = null;

            Base.RemoveHandler(InputElement.PointerPressedEvent, MouseDown);
            Base.RemoveHandler(InputElement.PointerReleasedEvent, MouseUp);
        }

        bool dragged = false;

        private void DragStarted(object sender, VectorEventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            dragged = false;
        }

        private void DragCompleted(object sender, VectorEventArgs e) {
            if (!dragged) Focused?.Invoke(this);
            else if (change != 0) Moved?.Invoke(this, 0, change);
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

        double change;

        private void MouseMove(object sender, VectorEventArgs e) {
            if (!dragged) change = 0;
            change += e.Vector.X;

            dragged = true;
            Moved?.Invoke(this, e.Vector.X, null);
        }
        
        public void Select() => this.Resources["Outline"] = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => this.Resources["Outline"] = new SolidColorBrush(new Color(0, 255, 255, 255));
    }
}
