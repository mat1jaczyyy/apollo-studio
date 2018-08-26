using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Chord: Device {
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= value && value <= 128)
                    _offset = value;
            }
        }

        public override Device Clone() {
            return new Chord(_offset);
        }

        public Chord() {
            this._offset = 0;
            this.MIDIExit = null;
        }

        public Chord(int offset) {
            this.Offset = offset;
            this.MIDIExit = null;
        }

        public Chord(Action<Signal> exit) {
            this._offset = 0;
            this.MIDIExit = exit;
        }

        public Chord(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (this.MIDIExit != null)
                this.MIDIExit(n);
            
            int result = (int)(n.p) + _offset;
            
            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }
}