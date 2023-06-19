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
    public class Refresh: Device {
        bool[] _macros = new bool[4];

        public bool GetMacro(int index) => _macros[index];
        public void SetMacro(int index, bool macro) {
            _macros[index] = macro;

            if (Viewer?.SpecificViewer != null) ((RefreshViewer)Viewer.SpecificViewer).SetMacro(index, macro);
        }
        
        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { _macros.ToArray() };

        public Refresh(bool[] macros = null): base("refresh") {
            if (macros == null || macros.Length != 4) macros = new bool[4];
            _macros = macros;
        }

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(i => {
                for (int j = 0; j < 4; j++) {
                    if (_macros[j])
                        i.Macros[j] = (int)Program.Project.GetMacro(j + 1);
                }
            });
            
            InvokeExit(n);
        }
        
        public class MacroUndoEntry: SimpleIndexPathUndoEntry<Refresh, bool> {
            protected override void Action(Refresh item, int index, bool element) => item.SetMacro(index, element);
            
            public MacroUndoEntry(Refresh refresh, int index, bool u, bool r)
            : base($"Refresh Macro {index + 1} changed to {(r? "Enabled" : "Disabled")}", refresh, index, u, r) {}
            
            MacroUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}