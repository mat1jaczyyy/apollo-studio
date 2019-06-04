using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Windows;

namespace Apollo.Components {
    public class Popout: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ClickedEventHandler();
        public event ClickedEventHandler Clicked;

        public Popout() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Clicked?.Invoke();
        }
    }
}
