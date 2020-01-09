using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Core;

namespace Apollo.Devices {
    public class Refresh: Device {

        public override Device Clone() => new Refresh() {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        bool[] _macros = new bool[4];
        public bool GetMacro(int index) => _macros[index];
        public void SetMacro(int index, bool macro) {
            _macros[index] = macro;
            if (Viewer?.SpecificViewer != null) ((RefreshViewer)Viewer.SpecificViewer).SetMacro(index, macro);
        }

        public Refresh(): base("refresh") {}

        public override void MIDIProcess(Signal n) {
            for (int i = 0; i < 4; i++) {
                if (_macros[i] == true) n.Macros[i] = (int)Program.Project.GetMacro(i+1);
            }
            
            InvokeExit(n);
        }
    }
}