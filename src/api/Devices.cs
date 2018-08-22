using System;
using System.Linq;

using api;

namespace api.Devices {
    public abstract class Device {
        public abstract void MIDIEnter(Communication.Note n);
        public Action<Communication.Note> MIDIExit;
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
        public Pitch(Action<Communication.Note> exit) {
            this.Offset = 0;
            this.MIDIExit = exit;
        }

        // Load device
        public Pitch(int offset, Action<Communication.Note> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Communication.Note n) {
            n.p += _offset;
            
            if (n.p < 0) n.p = 0;
            if (n.p > 127) n.p = 127;

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
        public Chord(Action<Communication.Note> exit) {
            this.Offset = 0;
            this.MIDIExit = exit;
        }

        // Load device
        public Chord(int offset, Action<Communication.Note> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Communication.Note n) {
            this.MIDIExit(n);
            
            n.p += _offset;
            
            if (n.p < 0) n.p = 0;
            if (n.p > 127) n.p = 127;

            this.MIDIExit(n);
        }
    }
}