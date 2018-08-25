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

        // Create device
        public Pitch(Action<Signal> exit) {
            this.Offset = 0;
            this.MIDIExit = exit;
        }

        // Load device
        public Pitch(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            int result = (int)(n.p);

            result += _offset;
            
            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

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

        // Create device
        public Chord(Action<Signal> exit) {
            this.Offset = 0;
            this.MIDIExit = exit;
        }

        // Load device
        public Chord(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            this.MIDIExit(n);
            
            int result = (int)(n.p);

            result += _offset;
            
            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            this.MIDIExit(n);
        }
    }
}