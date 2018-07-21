using System;
using System.Linq;

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
    }
}