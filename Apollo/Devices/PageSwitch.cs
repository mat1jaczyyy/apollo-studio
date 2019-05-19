using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class PageSwitch: Device {
        public static readonly new string DeviceIdentifier = "pageswitch";

        private int _target = 1;
        public int Target {
            get => _target;
            set {
                if (1 <= value && value <= 100 && _target != value) {
                    _target = value;
                    
                    if (Viewer?.SpecificViewer != null) ((PageSwitchViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        public override Device Clone() => new PageSwitch(Target);

        public PageSwitch(int target = 1): base(DeviceIdentifier) => Target = target;

        public override void MIDIEnter(Signal n) {
            Program.Project.Page = Target;
            MIDIExit?.Invoke(n);
        }
    }
}