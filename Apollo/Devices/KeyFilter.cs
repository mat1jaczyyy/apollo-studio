using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class KeyFilter: Device {
        bool[] _filter;
        public bool[] Filter {
            get => _filter;
            set {
                if (value != null && value.Length == 100) {
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
                if (1 <= index && index <= 99)
                    _filter[index] = value;
            }
        }

        public KeyFilter(bool[] init = null): base("keyfilter", "Key Filter") {
            if (init == null || init.Length != 100) init = new bool[100];
            _filter = init;
        }

        public override void MIDIProcess(Signal n) {
            if (_filter[n.Index])
                MIDIExit?.Invoke(n);
        }
    }
}