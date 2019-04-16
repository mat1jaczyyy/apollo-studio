using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class DeviceHead: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        public DeviceHead() => InitializeComponent();
    }
}
