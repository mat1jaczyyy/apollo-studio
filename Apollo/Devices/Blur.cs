using System;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Blur : Device {
        
        double _radius;
        public double Radius {
            get => _radius;
            set {
                _radius = value;
                
                if (Viewer?.SpecificViewer != null) ((BlurViewer)Viewer.SpecificViewer).SetRadius(Radius);
            }
        }
        double _amount;
        public double Amount {
            get => _amount;
            set {
                _amount = value;
                
                if (Viewer?.SpecificViewer != null) ((BlurViewer)Viewer.SpecificViewer).SetAmount(Amount);
            }
        }
        
        public override Device Clone() => new Blur(Radius, Amount);

        public Blur(double radius = 2, double amount = 0.5): base("blur") {
            Radius = radius;
            Amount = amount;
        }

        public override void MIDIProcess(Signal n) {
            int size = (int)Radius;
            
            int px = n.Index % 10;
            int py = n.Index / 10;
            
            for (int ax = -size; ax <= size; ax++) {
                for (int ay = -size; ay <= size; ay++) {
                    double distance = Math.Sqrt(Math.Pow(ax, 2) + Math.Pow(ay, 2));
                    if (Offset.Validate(px + ax, py + ay, GridType.Full, false, out int newIndex) && distance <= Radius && !(ax == 0 && ay == 0)) {
                        Color newColor = n.Color.Clone();
                        
                        double factor = Amount * (1 - distance / Radius);
                        
                        newColor.Red = (byte)(newColor.Red * factor);
                        newColor.Green = (byte)(newColor.Green * factor);
                        newColor.Blue = (byte)(newColor.Blue * factor);
                        
                        InvokeExit(n.With((byte)newIndex, newColor));
                    }
                }
            }
            
            InvokeExit(n);
        }
    }
}