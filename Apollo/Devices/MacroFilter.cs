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
        
        public class TargetUndoEntry: PathUndoEntry<MacroFilter> {
            int u, r;
            
            protected override void UndoPath(params MacroFilter[] items) => items[0].Macro = u;
            
            protected override void RedoPath(params MacroFilter[] items) => items[0].Macro = r;
            
            public TargetUndoEntry(MacroFilter MacroFilter, int u, int r)
            : base($"MacroFilter Target Changed to {r}%", MacroFilter){
                this.u = u;
                this.r = r;
            }
        }
        
        public class FilterUndoEntry: PathUndoEntry<MacroFilter> {
            bool[] u, r;
            
            protected override void UndoPath(params MacroFilter[] items) => items[0].Filter = u;
            
            protected override void RedoPath(params MacroFilter[] items) => items[0].Filter = r;
            
            public FilterUndoEntry(MacroFilter MacroFilter, bool[] u, bool[] r)
            : base($"MacroFilter Changed", MacroFilter){
                this.u = u;
                this.r = r;
            }
        }
    }
}