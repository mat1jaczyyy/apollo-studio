using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class MacroFilter: Device {
        bool[] _filter;
        public int Target;

        public override Device Clone() => new MacroFilter(_filter.ToArray()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public bool this[int index] {
            get => _filter[index];
            set {
                if (0 <= index && index <= 99 && _filter[index] != value) {
                    _filter[index] = value;
                    
                    if (Viewer?.SpecificViewer != null) ((MacroFilterViewer)Viewer.SpecificViewer).Set(index, _filter[index]);
                }
            }
        }

        public MacroFilter(bool[] init = null, int target = 1): base("macrofilter", "Macro Filter") {
            Target = target;
            if (init == null || init.Length != 100) {
                init = new bool[100];
                init[0] = true;
            }
            _filter = init;
        }

        public override void MIDIProcess(Signal n) {
            if (_filter[n.GetMacro(Target) - 1])
                InvokeExit(n);
        }
    }
}