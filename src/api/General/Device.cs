using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public abstract class Device {
        public abstract void MIDIEnter(Signal n);
        public abstract Device Clone();
        public Action<Signal> MIDIExit = null;
    }
}