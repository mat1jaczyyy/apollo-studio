using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class ColorFilter: Device {
        double _h, _s, _v, _th, _ts, _tv;

        public double Hue {
            get => _h;
            set {
                if (-180 <= value && value <= 180 && _h != value) {
                    _h = value;

                    if (Viewer?.SpecificViewer != null) ((ColorFilterViewer)Viewer.SpecificViewer).SetHue(Hue);
                }
            }
        }

        public double Saturation {
            get => _s;
            set {
                if (0 <= value && value <= 1 && _s != value) {
                    _s = value;

                    if (Viewer?.SpecificViewer != null) ((ColorFilterViewer)Viewer.SpecificViewer).SetSaturation(Saturation);
                }
            }
        }

        public double Value {
            get => _v;
            set {
                if (0 <= value && value <= 1 && _v != value) {
                    _v = value;

                    if (Viewer?.SpecificViewer != null) ((ColorFilterViewer)Viewer.SpecificViewer).SetValue(Value);
                }
            }
        }

        public double HueTolerance {
            get => _th;
            set {
                if (0 <= value && value <= 1 && _th != value) {
                    _th = value;

                    if (Viewer?.SpecificViewer != null) ((ColorFilterViewer)Viewer.SpecificViewer).SetHueTolerance(HueTolerance);
                }
            }
        }

        public double SaturationTolerance {
            get => _ts;
            set {
                if (0 <= value && value <= 1 && _ts != value) {
                    _ts = value;

                    if (Viewer?.SpecificViewer != null) ((ColorFilterViewer)Viewer.SpecificViewer).SetSaturationTolerance(SaturationTolerance);
                }
            }
        }

        public double ValueTolerance {
            get => _tv;
            set {
                if (0 <= value && value <= 1 && _tv != value) {
                    _tv = value;

                    if (Viewer?.SpecificViewer != null) ((ColorFilterViewer)Viewer.SpecificViewer).SetValueTolerance(ValueTolerance);
                }
            }
        }

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Hue, Saturation, Value, HueTolerance, SaturationTolerance, ValueTolerance };

        public ColorFilter(double hue = 0, double saturation = 1, double value = 1, double hue_t = 0.05, double saturation_t = 0.05, double value_t = 0.05): base("colorfilter", "Color Filter") {
            Hue = hue;
            Saturation = saturation;
            Value = value;

            HueTolerance = hue_t;
            SaturationTolerance = saturation_t;
            ValueTolerance = value_t;
        }

        public override void MIDIProcess(List<Signal> n)
            => InvokeExit(n.Where(i => {
                if (!i.Color.Lit) return true;

                (double hue, double saturation, double value) = i.Color.ToHSV();

                return (180 - Math.Abs(Math.Abs(hue - (Hue + 360) % 360) - 180)) / 180 <= HueTolerance &&
                        Math.Abs(saturation - Saturation) <= SaturationTolerance &&
                        Math.Abs(value - Value) <= ValueTolerance;
            }).ToList());

        public class HueUndoEntry: SimplePathUndoEntry<ColorFilter, double> {
            protected override void Action(ColorFilter item, double element) => item.Hue = element;
            
            public HueUndoEntry(ColorFilter colorFilter, double u, double r) 
            : base($"Color Filter Hue Changed to {r}°", colorFilter, u, r) {}
            
            HueUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class SaturationUndoEntry: SimplePathUndoEntry<ColorFilter, double> {
            protected override void Action(ColorFilter item, double element) => item.Saturation = element;
            
            public SaturationUndoEntry(ColorFilter colorFilter, double u, double r) 
            : base($"Color Filter Sat Changed to {r}%", colorFilter, u / 100, r / 100) {}
            
            SaturationUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ValueUndoEntry: SimplePathUndoEntry<ColorFilter, double> {
            protected override void Action(ColorFilter item, double element) => item.Value = element;
            
            public ValueUndoEntry(ColorFilter colorFilter, double u, double r) 
            : base($"Color Filter Value Changed to {r}%", colorFilter, u / 100, r / 100) {}
            
            ValueUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class HueToleranceUndoEntry: SimplePathUndoEntry<ColorFilter, double> {
            protected override void Action(ColorFilter item, double element) => item.HueTolerance = element;
            
            public HueToleranceUndoEntry(ColorFilter colorFilter, double u, double r) 
            : base($"Color Filter Hue Tol Changed to {r}%", colorFilter, u / 100, r / 100) {}
            
            HueToleranceUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class SaturationToleranceUndoEntry: SimplePathUndoEntry<ColorFilter, double> {
            protected override void Action(ColorFilter item, double element) => item.SaturationTolerance = element;
            
            public SaturationToleranceUndoEntry(ColorFilter colorFilter, double u, double r) 
            : base($"Color Filter Sat Tol Changed to {r}%", colorFilter, u / 100, r / 100) {}
            
            SaturationToleranceUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ValueToleranceUndoEntry: SimplePathUndoEntry<ColorFilter, double> {
            protected override void Action(ColorFilter item, double element) => item.ValueTolerance = element;
            
            public ValueToleranceUndoEntry(ColorFilter colorFilter, double u, double r) 
            : base($"Color Filter Value Tol Changed to {r}%", colorFilter, u / 100, r / 100) {}
            
            ValueToleranceUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}