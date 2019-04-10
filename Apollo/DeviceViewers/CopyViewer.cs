using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class CopyViewer: UserControl {
        public static readonly string DeviceIdentifier = "copy";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Copy _copy;

        Dial Rate, Gate;
        CheckBox Loop, Animate;

        public CopyViewer(Copy copy) {
            InitializeComponent();

            _copy = copy;

            Rate = this.Get<Dial>("Rate");
            Rate.UsingSteps = _copy.Mode;
            Rate.Length = _copy.Length;
            Rate.RawValue = _copy.Rate;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_copy.Gate * 100;

            Animate = this.Get<CheckBox>("Animate");
            Animate.IsChecked = _copy.Animate;
            Animate_Changed(null, EventArgs.Empty);

            Loop = this.Get<CheckBox>("Loop");
            Loop.IsChecked = _copy.Loop;
            Loop_Changed(null, EventArgs.Empty);
        }

        private void Rate_Changed(double value) => _copy.Rate = (int)value;

        private void Rate_ModeChanged(bool value) => _copy.Mode = value;

        private void Gate_Changed(double value) => _copy.Gate = (decimal)(value / 100);

        private void Animate_Changed(object sender, EventArgs e) {
            _copy.Animate = Animate.IsChecked.Value;
            Rate.Enabled = Gate.Enabled = Animate.IsChecked.Value;
        }

        private void Loop_Changed(object sender, EventArgs e) => _copy.Loop = Loop.IsChecked.Value;
    }
}
