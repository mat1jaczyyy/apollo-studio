using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Delay: Device {
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

        public Delay() {
            this._length = 200;
            this.MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length) {
            this.Length = length;
            this.MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(Action<Signal> exit) {
            this._length = 200;
            this.MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length, Action<Signal> exit) {
            this.Length = length;
            this.MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(Signal)) {
                Signal n = (Signal)info;
      
                if (this.MIDIExit != null)
                    this.MIDIExit(n);
                
                _timers.Dequeue();
            }
        }

        public override void MIDIEnter(Signal n) {
            _timers.Enqueue(new Timer(_timerexit, n, _length, System.Threading.Timeout.Infinite));
        }
    }
}