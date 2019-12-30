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

        public Refresh(): base("refresh") {}

        public override void MIDIProcess(Signal n) {
            n.Macros = (int[])Program.Project.Macros.Clone();
            InvokeExit(n);
        }
    }
}