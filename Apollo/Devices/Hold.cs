using System;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Hold: Device {
        public static readonly new string DeviceIdentifier = "hold";

        private int _time;
        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000 && _time != value) {
                    _time = value;
                    
                    if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetDurationValue(Time);
                }  
            }
        }

        private bool _mode; // true uses Length
        public bool Mode {
            get => _mode;
            set {
                if (_mode != value) {
                    _mode = value;
                    
                    if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetMode(Mode);
                }
            }
        }

        public Length Length;

        private void LengthChanged() {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetDurationStep(Length.Step);
        }

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        private bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                _infinite = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetInfinite(Infinite);
            }
        }

        private bool _release;
        public bool Release {
            get => _release;
            set {
                _release = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetRelease(Release);
            }
        }

        public override Device Clone() => new Hold(Mode, Length.Clone(), _time, _gate, Infinite, Release);

        public Hold(bool mode = false, Length length = null, int time = 1000, decimal gate = 1, bool infinite = false, bool release = false): base(DeviceIdentifier) {
            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Gate = gate;
            Infinite = infinite;
            Release = release;

            Length.Changed += LengthChanged;
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit ^ Release) {
                if (!Infinite) {
                    Courier courier = new Courier() {
                        Info = new Signal(Track.Get(this)?.Launchpad, n.Index, new Color(0), n.Layer),
                        AutoReset = false,
                        Interval = (double)((Mode? (int)Length : _time) * _gate),
                    };
                    courier.Elapsed += Tick;
                    courier.Start();
                }

                if (Release) n.Color = new Color();
                MIDIExit?.Invoke(n);
            }
        }
    }
}