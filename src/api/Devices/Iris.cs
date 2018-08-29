using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Iris: Device {
        private int _rate; // milliseconds
        private List<byte> _colors;
        private Queue<Timer> _timers;
        private TimerCallback _timerexit;

        public int Rate {
            get {
                return _rate;
            }
            set {
                if (0 <= value)
                    _rate = value;
            }
        }

        public override Device Clone() {
            return new Iris(_rate);
        }

        public Iris() {
            _rate = 200;
            _colors = new List<byte>();
            MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Iris(int rate) {
            Rate = rate;
            MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Iris(Action<Signal> exit) {
            _rate = 200;
            MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Iris(int rate, Action<Signal> exit) {
            Rate = rate;
            MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(byte)) {
                Signal n = new Signal((byte)info, new Color(0));
      
                if (MIDIExit != null)
                    MIDIExit(n);
                
                _timers.Dequeue();
            }
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit) {
                _timers.Enqueue(new Timer(_timerexit, n.Index, _rate, System.Threading.Timeout.Infinite));
                
                if (MIDIExit != null)
                    MIDIExit(n);
            }
        }
    }
}