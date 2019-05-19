using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class PageFilter: Device {
        public static readonly new string DeviceIdentifier = "pagefilter";

        private bool[] _filter;

        public override Device Clone() => new PageFilter(_filter.ToArray());

        public bool this[int index] {
            get => _filter[index];
            set {
                if (0 <= index && index <= 99 && _filter[index] != value) {
                    _filter[index] = value;
                    
                    if (Viewer?.SpecificViewer != null) ((PageFilterViewer)Viewer.SpecificViewer).Set(index, _filter[index]);
                }
            }
        }

        public PageFilter(bool[] init = null): base(DeviceIdentifier) {
            if (init == null || init.Length != 100) {
                init = new bool[100];
                init[Program.Project.Page - 1] = true;
            }
            _filter = init;
        }

        public override void MIDIEnter(Signal n) {
            if (_filter[Program.Project.Page - 1])
                MIDIExit?.Invoke(n);
        }
    }
}