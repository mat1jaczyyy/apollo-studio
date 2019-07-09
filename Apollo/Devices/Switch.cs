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
        
        bool _multireset;
        public bool MultiReset {
            get => _multireset;
            set {
                _multireset = value;
                
                if (Viewer?.SpecificViewer != null) ((SwitchViewer)Viewer.SpecificViewer).SetMultiReset(MultiReset);
            }
        }

        public override Device Clone() => new Switch(Page, MultiReset) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Switch(int page = 1, bool multireset = false): base("switch") {
            Page = page;
            MultiReset = multireset;
        }

        public override void MIDIProcess(Signal n) {
            if (!n.Color.Lit) {
                Program.Project.Page = Page;
                if (MultiReset) Multi.InvokeReset();
            }
            
            MIDIExit?.Invoke(n);
        }
    }
}