using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Device _device;

        public DeviceViewer(Device device) {
            _device = device;
            InitializeComponent();
        }
    }
}
