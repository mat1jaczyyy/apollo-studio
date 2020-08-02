using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    //+ Heaven compatible
    public class Rotate: Device {
        RotateType _mode;
        public RotateType Mode {
            get => _mode;
            set {
                _mode = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }

        bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        public override Device Clone() => new Rotate(Mode, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Rotate(RotateType mode = RotateType.D90, bool bypass = false): base("rotate") {
            Mode = mode;
            Bypass = bypass;
        }

        public override void MIDIProcess(IEnumerable<Signal> n) {
            /*if (Bypass) InvokeExit(n.Clone());
            
            if (Mode == RotateType.D90) {
                n.Index = (byte)((9 - n.Index % 10) * 10 + n.Index / 10);

            } else if (Mode == RotateType.D180) {
                n.Index = (byte)((9 - n.Index / 10) * 10 + 9 - n.Index % 10);

            } else if (Mode == RotateType.D270) {
                n.Index = (byte)((n.Index % 10) * 10 + 9 - n.Index / 10);
            }*/

            InvokeExit(n);
        }
        
        public class ModeUndoEntry: EnumSimplePathUndoEntry<Rotate, RotateType> {
            protected override void Action(Rotate item, RotateType element) => item.Mode = element;
            
            public ModeUndoEntry(Rotate rotate, RotateType u, RotateType r, IEnumerable source)
            : base("Rotate Angle", rotate, u, r, source) {}
            
            ModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}

        }
        
        public class BypassUndoEntry: SimplePathUndoEntry<Rotate, bool> {
            protected override void Action(Rotate item, bool element) => item.Bypass = element;
            
            public BypassUndoEntry(Rotate rotate, bool u, bool r)
            : base($"Rotate Bypass Changed to {(r? "Enabled" : "Disabled")}", rotate, u, r) {}
            
            BypassUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}