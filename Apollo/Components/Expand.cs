using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class Expand: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ClickedEventHandler(IPointerDevice e);
        public event ClickedEventHandler Clicked;

        public Expand() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Clicked?.Invoke(e.Device);
        }
    }
}
