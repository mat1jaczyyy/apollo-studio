using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class MacroFilter: Device {
        bool[] _filter;
        public bool[] Filter {
            get => _filter;
            set {
                if (value != null && value.Length == 100) {
                    _filter = value;

                    if (Viewer?.SpecificViewer != null) ((MacroFilterViewer)Viewer.SpecificViewer).Set(_filter);
                }
            }
        }

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
                if (0 <= index && index <= 99)
                    _filter[index] = value;
            }
        }

        public MacroFilter(int target = 1, bool[] init = null): base("macrofilter", "Macro Filter") {
            Macro = target;

            if (init == null || init.Length != 100) {
                init = new bool[100];
                init[Program.Project.GetMacro(Macro) - 1] = true;
            }
            
            _filter = init;
        }

        public override void MIDIProcess(Signal n) {
            if (_filter[n.GetMacro(Macro) - 1])
                InvokeExit(n);
        }
        
        public class TargetUndoEntry: SimpleUndoEntry<MacroFilter, int> {
            protected override void Action(MacroFilter item, int element) => item.Macro = element;
            
            public TargetUndoEntry(MacroFilter filter, int u, int r)
            : base($"Macro Filter Target Changed to {r}%", filter, u, r) {}
        }
        
        public class FilterUndoEntry: SimpleUndoEntry<MacroFilter, bool[]> {
            protected override void Action(MacroFilter item, bool[] element) => item.Filter = element.ToArray();
            
            public FilterUndoEntry(MacroFilter filter, bool[] u)
            : base($"Macro Filter Changed", filter, u.ToArray(), filter.Filter.ToArray()) {}
        }
    }
}