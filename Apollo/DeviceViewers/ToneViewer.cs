using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class ToneViewer: UserControl {
        public static readonly string DeviceIdentifier = "tone";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Tone _tone;

        public ToneViewer(Tone tone) {
            InitializeComponent();

            _tone = tone;

            this.Get<Dial>("Hue").RawValue = _tone.Hue;

            this.Get<Dial>("SaturationHigh").RawValue = _tone.SaturationHigh * 100;
            this.Get<Dial>("SaturationLow").RawValue = _tone.SaturationLow * 100;
            
            this.Get<Dial>("ValueHigh").RawValue = _tone.ValueHigh * 100;
            this.Get<Dial>("ValueLow").RawValue = _tone.ValueLow * 100;
        }

        private void Hue_Changed(double value) => _tone.Hue = value;

        private void SaturationHigh_Changed(double value) => _tone.SaturationHigh = value / 100;
        private void SaturationLow_Changed(double value) => _tone.SaturationLow = value / 100;

        private void ValueHigh_Changed(double value) => _tone.ValueHigh = value / 100;
        private void ValueLow_Changed(double value) => _tone.ValueLow = value / 100;
    }
}
