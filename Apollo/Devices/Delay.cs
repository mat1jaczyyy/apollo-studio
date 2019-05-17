using System;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Delay: Device {
        public static readonly new string DeviceIdentifier = "delay";

        private int _time;
        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000 && _time != value) {
                    _time = value;
                    
                    if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetDurationValue(Time);
                }  
            }
        }

        private bool _mode; // true uses Length
        public bool Mode {
            get => _mode;
            set {
                if (_mode != value) {
                    _mode = value;
                    
                    if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetMode(Mode);
                }
            }
        }

        public Length Length;

        private void LengthChanged() {
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetDurationStep(Length.Step);
        }

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        public override Device Clone() => new Delay(Mode, Length.Clone(), _time, _gate);

        public Delay(bool mode = false, Length length = null, int time = 1000, decimal gate = 1): base(DeviceIdentifier) {
            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Gate = gate;

            Length.Changed += LengthChanged;
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIEnter(Signal n) {
            Courier courier = new Courier() {
                Info = n.Clone(),
                AutoReset = false,
                Interval = (double)((Mode? (int)Length : _time) * _gate),
            };
            courier.Elapsed += Tick;
            courier.Start();
        }
    }
}