using System.Linq;

namespace api {
    public class Color {
        private byte _r, _g, _b;

        private bool IsValid(byte value) {
            return (0 <= value && value <= 63);
        }

        public byte Red {
            get {
                return _r;
            }
            set {
                if (0 <= value && value <= 63) _r = value;
            }
        }

        public byte Green {
            get {
                return _g;
            }
            set {
                if (0 <= value && value <= 63) _g = value;
            }
        }

        public byte Blue {
            get {
                return _b;
            }
            set {
                if (0 <= value && value <= 63) _b = value;
            }
        }

        public Color Clone() {
            return new Color(_r, _g, _b);
        }

        public Color() {
            _r = _g = _b = 63;
        }

        public Color(byte bright) {
            if (IsValid(bright))
                _r = _g = _b = bright;
        }

        public Color(byte red, byte green, byte blue) {
            Red = red;
            Green = green;
            Blue = blue;
        }
    }
}