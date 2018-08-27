namespace api {
    public class Signal {
        private byte _p = 11;
        private byte _r = 63;
        private byte _g = 63;
        private byte _b = 63;

        public byte Index {
            get {
                return _p;
            }
            set {
                if (0 <= value && value <= 127)
                    _p = value;
            }
        }

        public byte Red {
            get {
                return _r;
            }
            set {
                if (0 <= value && value <= 63)
                    _r = value;
            }
        }

        public byte Green {
            get {
                return _g;
            }
            set {
                if (0 <= value && value <= 63)
                    _g = value;
            }
        }
        public byte Blue {
            get {
                return _b;
            }
            set {
                if (0 <= value && value <= 63)
                    _b = value;
            }
        }

        public Signal Clone() {
            return new Signal(_p, _r, _g, _b);
        }

        public Signal() {}

        public Signal(byte index) {
            Index = index;
        }

        public Signal(byte index, byte brightness) {
            Index = index;
            Red = brightness;
            Green = brightness;
            Blue = brightness;
        }

        public Signal(byte index, byte red, byte green, byte blue) {
            Index = index;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public Signal(RtMidi.Core.Enums.Key index, int brightness) {
            Index = (byte)index;
            Red = (byte)brightness;
            Green = (byte)brightness;
            Blue = (byte)brightness;
        }

        public Signal(RtMidi.Core.Enums.Key index, int red, int green, int blue) {
            Index = (byte)index;
            Red = (byte)red;
            Green = (byte)green;
            Blue = (byte)blue;
        }
    }
}