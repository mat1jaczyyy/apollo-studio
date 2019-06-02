using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Paint: Device {
        public static readonly new string DeviceIdentifier = "paint";

        private Color _color;
        public Color Color {
            get => _color;
            set {
                if (_color != value) {
                    _color = value;

                    if (Viewer?.SpecificViewer != null) ((PaintViewer)Viewer.SpecificViewer).Set(Color);
                }
            }
        }

        public override Device Clone() => new Paint(Color.Clone());

        public Paint(Color color = null): base(DeviceIdentifier) => Color = color?? new Color(63);

        public override void MIDIProcess(Signal n) {
            if (n.Color.Lit) n.Color = Color.Clone();
            MIDIExit?.Invoke(n);
        }
    }
}