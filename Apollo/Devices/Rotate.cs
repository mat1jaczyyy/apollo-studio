using System;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Rotate: Device {

        bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }
        
        double _angle;
        
        public double Angle {
            get => _angle;
            set {
                _angle = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetAngle(Angle);
            }
        }

        public override Device Clone() => new Rotate(Angle, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Rotate(double angle = 0, bool bypass = false): base("rotate") {
            Bypass = bypass;
            Angle = angle;
        }

        public override void MIDIProcess(Signal n) {
            if (Bypass) InvokeExit(n.Clone());
            
            int preX, preY;
            preX = n.Index % 10;
            preY = n.Index / 10;
            
            double relX, relY;
            relX = (preX <= 4) ? preX - 5 : preX - 4;
            relY = (preY <= 4) ? preY - 5 : preY - 4;
            
            double x = Math.Round(Math.Cos(Angle) * relX + Math.Sin(Angle) * relY);
            double y = Math.Round(-Math.Sin(Angle) * relX + Math.Cos(Angle) * relY);
            
            int bx = (x > 0) ? 4 : 5;
            int by = (y > 0) ? 4 : 5;
            
            n.Index = (byte)(x + bx + (y + by) * 10);

            InvokeExit(n);
        }
    }
}