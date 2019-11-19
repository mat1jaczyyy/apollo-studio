using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Switch: Device {
        int _macro = 1;
        public int Macro {
            get => _macro;
            set {
                if (1 <= value && value <= 100 && _macro != value) {
                    _macro = value;
                    
                    if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetMacro(Macro);
                }
            }
        }

        public override Device Clone() => new Switch(Macro) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Switch(int macro = 1): base("switch") => Macro = macro;

        public override void MIDIProcess(Signal n) {
            if (!n.Color.Lit)
                Program.Project.Macro = Macro;
            
            InvokeExit(n);
        }
    }
}