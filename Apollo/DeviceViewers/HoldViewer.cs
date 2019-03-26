using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class HoldViewer: UserControl {
        public static readonly string DeviceIdentifier = "hold";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Hold _hold;

        public HoldViewer(Hold hold) {
            InitializeComponent();

            _hold = hold;
            this.Get<Dial>("Duration").RawValue = _hold.Time;
            this.Get<Dial>("Gate").RawValue = (double)_hold.Gate * 100;
        }

        private void Duration_Changed(double value) {
            _hold.Time = (int)value;
        }

        private void Gate_Changed(double value) {
            _hold.Gate = (decimal)(value / 100);
        }

        private void Infinite_Changed(object sender, EventArgs e) {
            _hold.Infinite = this.Get<CheckBox>("Infinite").IsChecked.Value;
        }
    }
}
