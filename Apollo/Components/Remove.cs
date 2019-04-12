using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class Remove: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void RemovedEventHandler();
        public event RemovedEventHandler Removed;
        
        public Remove() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Removed?.Invoke();
        }
    }
}
