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
    public class Switch: Device {
        int _target = 1;
        public int Target {
            get => _target;
            set {
                if (1 <= value && value <= 4 && _target != value) {
                    _target = value;
                    
                    if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }
        
        int _value = 1;
        public int Value {
            get => _value;
            set {
                if (1 <= value && value <= 100 && _value != value) {
                    _value = value;
                    
                    if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetValue(Value);
                }
            }
        }

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Target, Value };

        public Switch(int target = 1, int value = 1): base("switch") {
            Target = target;
            Value = value;
        }

        public override void MIDIProcess(List<Signal> n) {
            if (n.Any(i => !i.Color.Lit))
                Program.Project.SetMacro(Target, Value);

            InvokeExit(n);
        }
        
        public class TargetUndoEntry: SimplePathUndoEntry<Switch, int> {
            protected override void Action(Switch item, int element) => item.Target = element;
            
            public TargetUndoEntry(Switch macroswitch, int u, int r)
            : base($"Switch Target Changed to {r}", macroswitch, u, r) {}
            
            TargetUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ValueUndoEntry: SimplePathUndoEntry<Switch, int> {
            protected override void Action(Switch item, int element) => item.Value = element;
            
            public ValueUndoEntry(Switch macroswitch, int u, int r)
            : base($"Switch Value Changed to {r}", macroswitch, u, r) {}
            
            ValueUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}