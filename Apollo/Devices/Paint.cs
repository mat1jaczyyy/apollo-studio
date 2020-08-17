using System.Collections.Generic;
using System.IO;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

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

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(i => {
                if (i.Color.Lit)
                    i.Color = Color.Clone();
            });

            InvokeExit(n);
        }
        
        public class ColorUndoEntry: SimplePathUndoEntry<Paint, Color> {
            protected override void Action(Paint item, Color element) => item.Color = element.Clone();
            
            public ColorUndoEntry(Paint paint, Color u, Color r)
            : base($"Paint Color Changed to {r.ToHex()}", paint, u, r) {}
            
            ColorUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}