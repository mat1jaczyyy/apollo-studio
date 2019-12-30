using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Paint: Device {
        Color _color;
        public Color Color {
            get => _color;
            set {
                if (_color != value) {
                    _color = value;

                    if (Viewer?.SpecificViewer != null) ((PaintViewer)Viewer.SpecificViewer).Set(Color);
                }
            }
        }

        public override Device Clone() => new Paint(Color.Clone()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Paint(Color color = null): base("paint") => Color = color?? new Color();

        public override void MIDIProcess(Signal n) {
            if (n.Color.Lit) n.Color = Color.Clone();
            InvokeExit(n);
        }
    }
}