using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class Dial: UserControl {
        public Dial() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
