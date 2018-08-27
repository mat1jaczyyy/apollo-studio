using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Pitch: Device {
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
            return new Pitch(_offset);
        }

        public Pitch() {
            this._offset = 0;
            this.MIDIExit = null;
        }

        public Pitch(int offset) {
            this.Offset = offset;
            this.MIDIExit = null;
        }

        public Pitch(Action<Signal> exit) {
            this._offset = 0;
            this.MIDIExit = exit;
        }

        public Pitch(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            int result = n.Index + _offset;

            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.Index = (byte)result;

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }
}