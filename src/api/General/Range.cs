using System.Linq;

namespace api {
    public class Range {
        private byte _min = 0, _max = 127;

        public byte Min {
            get {
                return _min;
            }
            set {
                if (value >= 0 && value <= _max)
                    _min = value;
            }
        }

        public byte Max {
            get {
                return _max;
            }
            set {
                if (value <= 127 && value >= _min)
                    _max = value;
            }
        }

        public bool Check(byte value) {
            return _min <= value && value <= _max;
        }

        public Range() {}

        public Range(byte min, byte max) {
            Min = min;
            Max = max;
        }
    }
}