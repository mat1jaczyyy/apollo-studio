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

        Dial Duration, Gate;
        CheckBox Infinite;

        public HoldViewer(Hold hold) {
            InitializeComponent();

            _hold = hold;

            Duration = this.Get<Dial>("Duration");
            Duration.UsingSteps = _hold.Mode;
            Duration.Length = _hold.Length;
            Duration.RawValue = _hold.Time;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_hold.Gate * 100;

            Infinite = this.Get<CheckBox>("Infinite");
            Infinite.IsChecked = _hold.Infinite;
            Infinite_Changed(null, EventArgs.Empty);
        }

        private void Duration_Changed(double value) => _hold.Time = (int)value;

        private void Duration_ModeChanged(bool value) => _hold.Mode = value;

        private void Gate_Changed(double value) => _hold.Gate = (decimal)(value / 100);

        private void Infinite_Changed(object sender, EventArgs e) {
            _hold.Infinite = Infinite.IsChecked.Value;
            Duration.Enabled = Gate.Enabled = !Infinite.IsChecked.Value;
        }
    }
}
