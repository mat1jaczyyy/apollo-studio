using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class KeyFilter: Device {
        public static readonly new string DeviceIdentifier = "keyfilter";

        private bool[] _filter;

        public override Device Clone() => new KeyFilter(_filter.ToArray());

        public bool this[int index] {
            get => _filter[index];
            set {
                if (1 <= index && index <= 99)
                    _filter[index] = value;
            }
        }

        public KeyFilter(bool[] init = null): base(DeviceIdentifier) {
            if (init == null || init.Length != 100) init = new bool[100];
            _filter = init;
        }

        public override void MIDIEnter(Signal n) {
            if (_filter[n.Index])
                MIDIExit?.Invoke(n);
        }
    }
}