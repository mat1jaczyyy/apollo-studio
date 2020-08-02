using System;
using System.Collections.Generic;

using Apollo.Structures;

namespace Apollo.Elements {
    public abstract class SignalReceiver {
        public virtual Action<IEnumerable<Signal>> MIDIExit { get; set; }

        public abstract void MIDIEnter(IEnumerable<Signal> n);
        
        public void MIDIEnter(Signal n) => MIDIEnter(new [] {n});
    }
}