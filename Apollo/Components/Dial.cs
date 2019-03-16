using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class Dial: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        public Dial() {
            InitializeComponent();
        }
    }
}
