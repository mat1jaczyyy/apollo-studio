using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class DelayViewer: UserControl {
        public static readonly string DeviceIdentifier = "delay";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Delay _delay;

        public DelayViewer(Delay delay) {
            InitializeComponent();

            _delay = delay;
            this.Get<Dial>("Duration").RawValue = _delay.Time;
            this.Get<Dial>("Gate").RawValue = (double)_delay.Gate * 100;
        }

        private void Duration_Changed(double value) => _delay.Time = (int)value;

        private void Gate_Changed(double value) => _delay.Gate = (decimal)(value / 100);
    }
}
