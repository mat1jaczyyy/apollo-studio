using System;
using System.Linq;

using api;

namespace api.Devices {
    public abstract class Device {
        public abstract void MIDIEnter(Signal n);
        public Action<Signal> MIDIExit;
    }

    public class Pitch: Device {
        // Note offset
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= _offset && _offset <= 128) _offset = value;
            }
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
            int result = (int)(n.p) + _offset;

            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }

    public class Chord: Device {
        // Note offset
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= _offset && _offset <= 128) _offset = value;
            }
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