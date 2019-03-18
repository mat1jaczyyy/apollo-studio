using System;

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

        private void Duration_Changed(double value) {
            _device.Time = (int)value;
        }

        private void Gate_Changed(double value) {
            _device.Gate = (decimal)(value / 100);
        }
    }
}
