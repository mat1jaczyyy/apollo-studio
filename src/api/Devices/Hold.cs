using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Hold: Device {
        private int _length; // milliseconds
        private Queue<Timer> _timers;
        private TimerCallback _timerexit;

        public int Length {
            get {
                return _length;
            }
            set {
                if (0 <= value)
                    _length = value;
            }
        }

        public override Device Clone() {
            return new Delay(_length);
        }

        public Hold() {
            _length = 200;
            MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Hold(int length) {
            Length = length;
            MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Hold(Action<Signal> exit) {
            _length = 200;
            MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Hold(int length, Action<Signal> exit) {
            Length = length;
            MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(byte)) {
                Signal n = new Signal((byte)info, 0);
      
                if (MIDIExit != null)
                    MIDIExit(n);
                
                _timers.Dequeue();
            }
        }

        public override void MIDIEnter(Signal n) {
            if (n.Pressed) {
                _timers.Enqueue(new Timer(_timerexit, n.Index, _length, System.Threading.Timeout.Infinite));
                
                if (MIDIExit != null)
                    MIDIExit(n);
            }
        }
    }
}