using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Switch: Device {
        int _value = 1;
        int _target = 1;
        
        public int Value {
            get => _value;
            set {
                if (1 <= value && value <= 100 && _value != value) {
                    _value = value;
                    
                    if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetValueDial(Value);
                }
            }
        }
        
        public int Target {
            get => _target;
            set {
                if (1 <= value && value <= 4 && _target != value) {
                    _target = value;
                    
                    if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetTargetDial(Target);
                }
            }
        }

        public override Device Clone() => new Switch(Target, Value) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Switch(int target = 1, int value = 1): base("switch"){
            Value = value;
            Target = target;
        }

        public override void MIDIProcess(Signal n) {
            if (!n.Color.Lit)
                Program.Project.SetMacro(Target, Value);            
            InvokeExit(n);
        }
    }
}