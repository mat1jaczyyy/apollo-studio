using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class DelayViewer: UserControl {
        public static readonly string DeviceIdentifier = "delay";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Delay _device;

        public DelayViewer(Delay device) {
            InitializeComponent();

            _device = device;
        }
    }
}
