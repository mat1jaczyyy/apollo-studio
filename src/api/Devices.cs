using System;
using System.Linq;

using api;

namespace api.Devices {
    public abstract class Device {
        public abstract void MIDIEnter(Communication.Note n);
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
        public Pitch() {
            this.Offset = 0;
        }

        // Load device
        public Pitch(int offset) {
            this.Offset = offset;
        }

        public override void MIDIEnter(Communication.Note n) {
            n.p += _offset;
            
            if (n.p < 0) n.p = 0;
            if (n.p > 127) n.p = 127;

            Console.WriteLine(n.p);
        }
    }
}