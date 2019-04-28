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

            this.Get<Dial>("Hue").RawValue = (double)_tone.Hue;
            this.Get<Dial>("Saturation").RawValue = (double)_tone.Saturation * 100;
            this.Get<Dial>("Value").RawValue = (double)_tone.Value * 100;
        }

        private void Hue_Changed(double value) => _tone.Hue = (int)value;

        private void Saturation_Changed(double value) => _tone.Saturation = (decimal)(value / 100);

        private void Value_Changed(double value) => _tone.Value = (decimal)(value / 100);
    }
}
