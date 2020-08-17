using System;
using System.Collections.Generic;

using Apollo.Structures;

namespace Apollo.Elements {
    public abstract class SignalReceiver {
        public virtual Action<List<Signal>> MIDIExit { get; set; }

        public abstract void MIDIEnter(List<Signal> n);
        
        public void MIDIEnter(Signal n) => MIDIEnter(new List<Signal> { n });
    }
}