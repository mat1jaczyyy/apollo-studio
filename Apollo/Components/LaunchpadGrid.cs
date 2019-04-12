using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

namespace Apollo.Components {
    public class LaunchpadGrid: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        UniformGrid Grid;
        Shape ModeLight;

        public delegate void PadChangedEventHandler(int index);
        public event PadChangedEventHandler PadPressed;
        public event PadChangedEventHandler PadReleased;

        public static int GridToSignal(int index) => (index == -1)? 99 : ((9 - (index / 10)) * 10 + index % 10);
        public static int SignalToGrid(int index) => (index == 99)? -1 : ((9 - (index / 10)) * 10 + index % 10);

        public void SetColor(int index, SolidColorBrush color) {
            if (index == 0 || index == 9 || index == 90 || index == 99) return;

            if (index == -1) ModeLight.Fill = color;
            else ((Shape)Grid.Children[index]).Fill = color;
        }

        public LaunchpadGrid() {
            InitializeComponent();

            Grid = this.Get<UniformGrid>("LaunchpadGrid");
            ModeLight = this.Get<Rectangle>("ModeLight");
        }

        bool mouseHeld = false;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;
                MouseEnter(sender, new PointerEventArgs());
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                MouseLeave(sender, new PointerEventArgs());
                mouseHeld = false;
            }
        }

        private void MouseEnter(object sender, PointerEventArgs e) {
            if (mouseHeld) PadPressed?.Invoke(Grid.Children.IndexOf((IControl)sender));
        }

        private void MouseLeave(object sender, PointerEventArgs e) {
            if (mouseHeld) PadReleased?.Invoke(Grid.Children.IndexOf((IControl)sender));
        }
    }
}
