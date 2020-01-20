using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Blur : Device {
        ConcurrentDictionary<int, List<Signal>> currentSignals = new ConcurrentDictionary<int, List<Signal>>();
        object locker = new object();
        
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
            lock (locker) {
                currentSignals[n.Index] = new List<Signal>();
                
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
                            
                            currentSignals[n.Index].Add(n.With((byte)newIndex, newColor));
                        }
                    }
                }
                
                currentSignals[n.Index].Add(n);
                
                ScreenOutput();
            }
        }
        
        void ScreenOutput() {
            lock (locker)
            {
                List<Signal> outputSignals = new List<Signal>(101);
                for(int i = 0; i < 101; i++) outputSignals.Add(null);
                
                foreach (int inputIndex in currentSignals.Keys) {
                    foreach (Signal blurSignal in currentSignals[inputIndex].ToList()) {
                        byte index = blurSignal.Index;
                        
                        if (ReferenceEquals(outputSignals.ElementAtOrDefault(index), null)) outputSignals[index] = blurSignal.Clone();
                        else
                        {
                            Color color = outputSignals[index].Color;

                            color.Red += blurSignal.Color.Red;
                            color.Blue += blurSignal.Color.Blue;
                            color.Green += blurSignal.Color.Green;

                            outputSignals[index] = outputSignals[index].With(index, color);
                        }
                    }
                }
                
                foreach(Signal signal in outputSignals) {
                    if(!ReferenceEquals(signal, null)) InvokeExit(signal.Clone());
                }
            }
        }
        
        protected override void Stop() {
            currentSignals.Clear();
        }
    }
}