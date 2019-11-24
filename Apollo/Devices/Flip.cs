using System;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Flip: Device {
        double _angle;
        public double Angle {
            get => _angle;
            set {
                _angle = value;

                if (Viewer?.SpecificViewer != null) ((FlipViewer)Viewer.SpecificViewer).SetAngle(Angle);
            }
        }

        bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((FlipViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        public override Device Clone() => new Flip(Angle, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Flip(double angle = 0, bool bypass = false): base("flip") {
            Angle = angle;
            Bypass = bypass;
        }

        public override void MIDIProcess(Signal n) {
            if (Bypass) InvokeExit(n.Clone());
            
            int preX, preY;
            preX = n.Index % 10;
            preY = n.Index / 10;

            double relX, relY;
            relX = (preX <= 4) ? preX - 5 : preX - 4;
            relY = (preY <= 4) ? preY - 5 : preY - 4;
            
            double m11 = Math.Pow(Math.Cos(Angle), 2) - Math.Pow(Math.Sin(Angle), 2);
            double m12 = 2 * Math.Cos(Angle) * Math.Sin(Angle);
            double m22 = Math.Pow(Math.Sin(Angle), 2) - Math.Pow(Math.Cos(Angle), 2);

            double x = Math.Round(m11 * relX + m12 * relY);
            double y = Math.Round(m12 * relX + m22 * relY);

            int bx = (x > 0) ? 4 : 5;
            int by = (y > 0) ? 4 : 5;

            n.Index = (byte)(x + bx + (y + by) * 10);
            
            InvokeExit(n);
        }
    }
}