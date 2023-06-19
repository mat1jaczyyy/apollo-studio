using System.Collections.Generic;
using System.IO;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Tone: Device {
        double _h, _sh, _sl, _vh, _vl;

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

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Hue, SaturationHigh, SaturationLow, ValueHigh, ValueLow };

        public Tone(double hue = 0, double saturation_high = 1, double saturation_low = 0, double value_high = 1, double value_low = 0): base("tone") {
            Hue = hue;

            SaturationHigh = saturation_high;
            SaturationLow = saturation_low;

            ValueHigh = value_high;
            ValueLow = value_low;
        }

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(i => {
                if (i.Color.Lit) {
                    (double hue, double saturation, double value) = i.Color.ToHSV();

                    hue = (hue + Hue + 360) % 360;
                    saturation = saturation * (SaturationHigh - SaturationLow) + SaturationLow;
                    value = value * (ValueHigh - ValueLow) + ValueLow;

                    i.Color = Color.FromHSV(hue, saturation, value);
                }
            });

            InvokeExit(n);
        }
        
        public class HueUndoEntry: SimplePathUndoEntry<Tone, double> {
            protected override void Action(Tone item, double element) => item.Hue = element;
            
            public HueUndoEntry(Tone tone, double u, double r)
            : base($"Tone Hue Changed to {r}°", tone, u, r) {}
            
            HueUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class SatHighUndoEntry: SimplePathUndoEntry<Tone, double> {
            protected override void Action(Tone item, double element) => item.SaturationHigh = element;
            
            public SatHighUndoEntry(Tone tone, double u, double r)
            : base($"Tone Sat Hi Changed to {r}%", tone, u / 100, r / 100) {}
            
            SatHighUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class SatLowUndoEntry: SimplePathUndoEntry<Tone, double> {
            protected override void Action(Tone item, double element) => item.SaturationLow = element;
            
            public SatLowUndoEntry(Tone tone, double u, double r)
            : base($"Tone Sat Lo Changed to {r}%", tone, u / 100, r / 100) {}
            
            SatLowUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ValueHighUndoEntry: SimplePathUndoEntry<Tone, double> {
            protected override void Action(Tone item, double element) => item.ValueHigh = element;
            
            public ValueHighUndoEntry(Tone tone, double u, double r)
            : base($"Tone Value Hi Changed to {r}%", tone, u / 100, r / 100) {}
            
            ValueHighUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ValueLowUndoEntry: SimplePathUndoEntry<Tone, double> {
            protected override void Action(Tone item, double element) => item.ValueLow = element;
            
            public ValueLowUndoEntry(Tone tone, double u, double r)
            : base($"Tone Value Lo Changed to {r}%", tone, u / 100, r / 100) {}
            
            ValueLowUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}