using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Tone: Device {
        private double _h, _sh, _sl, _vh, _vl;

        public double Hue {
            get => _h;
            set {
                if (-180 <= value && value <= 180 && _h != value) {
                    _h = value;

                    if (Viewer?.SpecificViewer != null) ((ToneViewer)Viewer.SpecificViewer).SetHue(Hue);
                }
            }
        }

        public double SaturationHigh {
            get => _sh;
            set {
                if (0 <= value && value <= 1 && _sh != value) {
                    _sh = value;

                    if (Viewer?.SpecificViewer != null) ((ToneViewer)Viewer.SpecificViewer).SetSaturationHigh(SaturationHigh);
                }
            }
        }

        public double SaturationLow {
            get => _sl;
            set {
                if (0 <= value && value <= 1 && _sl != value) {
                    _sl = value;

                    if (Viewer?.SpecificViewer != null) ((ToneViewer)Viewer.SpecificViewer).SetSaturationLow(SaturationLow);
                }
            }
        }

        public double ValueHigh {
            get => _vh;
            set {
                if (0 <= value && value <= 1 && _vh != value) {
                    _vh = value;

                    if (Viewer?.SpecificViewer != null) ((ToneViewer)Viewer.SpecificViewer).SetValueHigh(ValueHigh);
                }
            }
        }

        public double ValueLow {
            get => _vl;
            set {
                if (0 <= value && value <= 1 && _vl != value) {
                    _vl = value;

                    if (Viewer?.SpecificViewer != null) ((ToneViewer)Viewer.SpecificViewer).SetValueLow(ValueLow);
                }
            }
        }

        public override Device Clone() => new Tone(Hue, SaturationHigh, SaturationLow, ValueHigh, ValueLow) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Tone(double hue = 0, double saturation_high = 1, double saturation_low = 0, double value_high = 1, double value_low = 0): base("tone") {
            Hue = hue;

            SaturationHigh = saturation_high;
            SaturationLow = saturation_low;

            ValueHigh = value_high;
            ValueLow = value_low;
        }

        public override void MIDIProcess(Signal n) {
            if (n.Color.Lit) {
                (double hue, double saturation, double value) = n.Color.ToHSV();

                hue = (hue + Hue) % 360;
                saturation = saturation * (SaturationHigh - SaturationLow) + SaturationLow;
                value = value * (ValueHigh - ValueLow) + ValueLow;

                n.Color = Color.FromHSV(hue, saturation, value);
            }

            MIDIExit?.Invoke(n);
        }
    }
}