using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    //+ Heaven compatible
    public class Clear: Device {
        ClearType _mode;
        public ClearType Mode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((ClearViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }
        
        public override Device Clone() => new Clear(Mode) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Clear(ClearType mode = ClearType.Lights): base("clear") => Mode = mode;

        public override IEnumerable<Signal> MIDIProcess(IEnumerable<Signal> n) {
            if (n.Any(i => !i.Color.Lit)) {  // TODO If unlit signal has delay, then this go big gay
                if (Mode == ClearType.Multi) Multi.InvokeReset();
                else MIDI.ClearState(multi: Mode == ClearType.Both);
            }

            return n;
        }
        
        public class ModeUndoEntry: SimplePathUndoEntry<Clear, ClearType> {
            protected override void Action(Clear item, ClearType element) => item.Mode = element;
            
            public ModeUndoEntry(Clear clear, ClearType u, ClearType r)
            : base($"Clear Mode Changed to {r.ToString()}", clear, u, r) {}
            
            ModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}