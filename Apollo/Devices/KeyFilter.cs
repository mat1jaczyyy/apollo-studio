using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    //+ Heaven compatible
    public class KeyFilter: Device {
        bool[] _filter;
        public bool[] Filter {
            get => _filter;
            set {
                if (value != null && value.Length == 101) {
                    _filter = value;

                    if (Viewer?.SpecificViewer != null) ((KeyFilterViewer)Viewer.SpecificViewer).Set(_filter);
                }
            }
        }

        public override Device Clone() => new KeyFilter(_filter.ToArray()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public bool this[int index] {
            get => _filter[index];
            set {
                if (0 <= index && index <= 100)
                    _filter[index] = value;
            }
        }

        public KeyFilter(bool[] init = null): base("keyfilter", "Key Filter") {
            if (init == null || init.Length != 101) init = new bool[101];
            _filter = init;
        }

        public override void MIDIProcess(List<Signal> n) {
            /*if (_filter[n.Index])*/
                InvokeExit(n);
        }
        
        public class ChangedUndoEntry: SimplePathUndoEntry<KeyFilter, bool[]> {
            protected override void Action(KeyFilter item, bool[] element) => item.Filter = element.ToArray();
            
            public ChangedUndoEntry(KeyFilter filter, bool[] u)
            : base($"Key Filter Changed", filter, u.ToArray(), filter.Filter.ToArray()) {}
            
            ChangedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}