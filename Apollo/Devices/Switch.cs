using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
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

        public override Device Clone() => new Switch(Target, Value) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Switch(int target = 1, int value = 1): base("switch") {
            Target = target;
            Value = value;
        }

        public override void MIDIProcess(Signal n) {
            if (!n.Color.Lit)
                Program.Project.SetMacro(Target, Value); 

            InvokeExit(n);
        }
        
        public class TargetUndoEntry: PathUndoEntry<Switch> {
            int u, r;
            
            protected override void UndoPath(params Switch[] items) => items[0].Target = u;
            
            protected override void RedoPath(params Switch[] items) => items[0].Target = r;
            
            public TargetUndoEntry(Switch rotate, int u, int r)
            : base($"Switch Target Changed to {r}", rotate){
                this.u = u;
                this.r = r;
            }
        }
        
        public class ValueUndoEntry: PathUndoEntry<Switch> {
            int u, r;
            
            protected override void UndoPath(params Switch[] items) => items[0].Value = u;
            
            protected override void RedoPath(params Switch[] items) => items[0].Value = r;
            
            public ValueUndoEntry(Switch rotate, int u, int r)
            : base($"Switch Value Changed to {r}", rotate){
                this.u = u;
                this.r = r;
            }
        }
    }
}