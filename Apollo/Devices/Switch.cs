using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Switch: Device {
        int _page = 1;
        public int Page {
            get => _page;
            set {
                if (1 <= value && value <= 100 && _page != value) {
                    _page = value;
                    
                    if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetPage(Page);
                }
            }
        }

        public override Device Clone() => new Switch(Page) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Switch(int page = 1): base("switch") => Page = page;

        public override void MIDIProcess(Signal n) {
            if (!n.Color.Lit)
                Program.Project.Page = Page;
            
            MIDIExit?.Invoke(n);
        }
    }
}