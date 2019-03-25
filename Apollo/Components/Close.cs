using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class Close: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ClickEventHandler();
        public event ClickEventHandler Click;

        public Close() {
            InitializeComponent();
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left)
                Click?.Invoke();
        }
    }
}
