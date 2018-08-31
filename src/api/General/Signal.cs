using api;

namespace api {
    public class Signal {
        private byte _p = 11;
        public Color Color = new Color(63);

        public byte Index {
            get {
                return _p;
            }
            set {
                if (0 <= value && value <= 127)
                    _p = value;
            }
        }

        public Signal Clone() {
            return new Signal(_p, Color.Clone());
        }

        public Signal() {}

        public Signal(Color color) {
            Color = color;
        }

        public Signal(byte index) {
            Index = index;
        }

        public Signal(byte index, Color color) {
            Index = index;
            Color = color;
        }

        public Signal(RtMidi.Core.Enums.Key index) {
            Index = (byte)index;
        }

        public Signal(RtMidi.Core.Enums.Key index, Color color) {
            Index = (byte)index;
            Color = color;
        }

        public override string ToString() {
            return $"{Index} / {Color.Red}, {Color.Green}, {Color.Blue}";
        }
    }
}