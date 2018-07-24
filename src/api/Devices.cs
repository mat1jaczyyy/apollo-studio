using System;
using System.Linq;

using api;

namespace api.Devices {
    public class Pitch {
        // Note offset
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= _offset && _offset <= 128)
                    _offset = value;
            }
        }

        // Create device
        public Pitch() {}

        // Load device
        public Pitch(int offset) {
            this.Offset = offset;
        }

        public Communication.Note Process(Communication.Note n) {
            n.p += _offset;
            
            if (n.p < 0) n.p = 0;
            if (n.p > 127) n.p = 127;

            return n;
        }
    }
}