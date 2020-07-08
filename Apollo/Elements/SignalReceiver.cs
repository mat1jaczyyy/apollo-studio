using System.Collections.Generic;

using Apollo.Structures;

namespace Apollo.Elements {
    public abstract class SignalReceiver {
        public abstract IEnumerable<Signal> MIDIEnter(IEnumerable<Signal> n);
        
        public IEnumerable<Signal> MIDIEnter(Signal n) => MIDIEnter(new [] {n});
    }
}