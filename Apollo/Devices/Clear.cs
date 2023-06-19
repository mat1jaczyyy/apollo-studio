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
    public class Clear: Device {
        ClearType _mode;
        public ClearType Mode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((ClearViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }
        
        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Mode };

        public Clear(ClearType mode = ClearType.Lights): base("clear") => Mode = mode;

        public override void MIDIProcess(List<Signal> n) {
            if (n.Any(i => !i.Color.Lit)) {
                if (Mode == ClearType.Multi) Multi.InvokeReset();
                else MIDI.ClearState(multi: Mode == ClearType.Both);
            }

            InvokeExit(n);
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