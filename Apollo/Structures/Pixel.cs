using System;
using System.Collections.Generic;

namespace Apollo.Structures {
    public class Pixel {
        public Action<Signal> MIDIExit = null;
        
        private SortedList<int, Signal> _signals = new SortedList<int, Signal>();
        private Color state = new Color(0);

        private object locker = new object();

        public Pixel() {}

        public void MIDIEnter(Signal n) {
            lock (locker) {
                int layer = -n.Layer;

                if (n.Color.Lit) _signals[layer] = n.Clone();
                else if (_signals.ContainsKey(layer)) _signals.Remove(layer);
                else return;

                Color newState = new Color(0);

                for (int i = 0; i < _signals.Count; i++) {
                    Signal signal = _signals.Values[i];

                    if (signal.BlendingMode == Signal.BlendingType.Mask) break;

                    newState.Mix(signal.Color);

                    if (signal.BlendingMode == Signal.BlendingType.Normal) break;
                }
                
                if (newState != state) {
                    Signal m = n.Clone();
                    m.Color = state = newState;
                    MIDIExit?.Invoke(m);
                }
            }
        }
    }
}