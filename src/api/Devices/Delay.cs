using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Delay: Device {
        private int _length = 200; // milliseconds
        private Queue<Timer> _timers = new Queue<Timer>();
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

        public Delay() {
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length) {
            Length = length;
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(Action<Signal> exit) {
            MIDIExit = exit;
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length, Action<Signal> exit) {
            Length = length;
            MIDIExit = exit;
            _timerexit = new TimerCallback(Tick);
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(Signal)) {
                Signal n = (Signal)info;
      
                if (MIDIExit != null)
                    MIDIExit(n);
                
                _timers.Dequeue();
            }
        }

        public override void MIDIEnter(Signal n) {
            _timers.Enqueue(new Timer(_timerexit, n.Clone(), _length, System.Threading.Timeout.Infinite));
        }
    }
}