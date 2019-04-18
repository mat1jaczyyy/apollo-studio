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
        Remove Remove;

        public IBrush Fill {
            get => (IBrush)this.Resources["Color"];
            set => this.Resources["Color"] = value;
        }

        private bool _removable = true;
        public bool Removable {
            get => _removable;
            set => Remove.Opacity = (_removable = value)? 1 : 0;
        }

        public FadeThumb() {
            InitializeComponent();
            
            Base = this.Get<Thumb>("Thumb");
            Remove = this.Get<Remove>("Remove");
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

        private void Removed() => Deleted?.Invoke(this);

        public void Select() => this.Resources["Outline"] = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => this.Resources["Outline"] = new SolidColorBrush(new Color(0, 255, 255, 255));
    }
}
