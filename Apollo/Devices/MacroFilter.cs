using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class MacroFilter: Device {
        bool[] _filter;
        int _macro;
        
        public int Macro {
            get => _macro;
            set {
                if (_macro != value && 1 <= value && value <= 4) {
                   _macro = value;

                   if (Viewer?.SpecificViewer != null) ((MacroFilterViewer)Viewer.SpecificViewer).SetMacro(Macro);
                }
            }
        }

        public override Device Clone() => new MacroFilter(Macro, _filter.ToArray()) {
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

        public MacroFilter(int target = 1, bool[] init = null): base("macrofilter", "Macro Filter") {
            Macro = target;

            if (init == null || init.Length != 100) {
                init = new bool[100];
                init[0] = true;
            }
            
            _filter = init;
        }

        public override void MIDIProcess(Signal n) {
            if (_filter[n.GetMacro(Macro) - 1])
                InvokeExit(n);
        }
    }
}