using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Infinity: Device {
        public override Device Clone() {
            return new Infinity();
        }

        public Infinity() {
            MIDIExit = null;
        }

        public Infinity(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit)
                if (MIDIExit != null)
                    MIDIExit(n);
        }
    }
}