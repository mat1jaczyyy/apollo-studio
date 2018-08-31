using api;

namespace api {
    public class Signal {
        private byte _p = 11;
        public Color Color = new Color(63);
        private int _l = 0;

        public byte Index {
            get {
                return _p;
            }
            set {
                if (0 <= value && value <= 127)
                    _p = value;
            }
        }

        public int Layer {
            get {
                return _l;
            }
            set {
                if (0 <= value)
                    _l = value;
            }
        }

        public Signal Clone() {
            return new Signal(_p, Color.Clone(), _l);
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

        public Signal(int layer) {
            Layer = layer;
        }

        public Signal(Color color, int layer) {
            Color = color;
            Layer = layer;
        }

        public Signal(byte index, int layer) {
            Index = index;
            Layer = layer;
        }

        public Signal(byte index, Color color, int layer) {
            Index = index;
            Color = color;
            Layer = layer;
        }

        public Signal(RtMidi.Core.Enums.Key index, int layer) {
            Index = (byte)index;
            Layer = layer;
        }

        public Signal(RtMidi.Core.Enums.Key index, Color color, int layer) {
            Index = (byte)index;
            Color = color;
            Layer = layer;
        }

        public override string ToString() {
            return $"{Index} / {Color.Red}, {Color.Green}, {Color.Blue}";
        }
    }
}