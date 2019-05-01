using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Delay: Device {
        public static readonly new string DeviceIdentifier = "delay";

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;
        private decimal _gate;

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

        public override Device Clone() => new Delay(Mode, Length.Clone(), _time, _gate);

        public Delay(bool mode = false, Length length = null, int time = 1000, decimal gate = 1): base(DeviceIdentifier) {
            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Gate = gate;
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