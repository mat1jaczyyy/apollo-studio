using System;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Hold: Device {
        public static readonly new string DeviceIdentifier = "hold";

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;
        private decimal _gate;
        public bool Infinite;
        public bool Release;

        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4)
                    _gate = value;
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
                        Info = new Signal(Track.Get(this).Launchpad, n.Index, new Color(0), n.Layer),
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