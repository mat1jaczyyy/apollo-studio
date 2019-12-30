using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Refresh: Device {

        public override Device Clone() => new Refresh() {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Refresh();

        public override void MIDIProcess(Signal n) {
            
            InvokeExit(n);
        }
    }
}