using System;
using System.Collections.Generic;
using System.Linq;

namespace api {
    public class Pixel {
        private SortedList<int, Signal> _signals = new SortedList<int, Signal>();
        private int? _highest = null;
        public Action<Signal> MIDIExit = null;

        public Pixel() {}

        public Pixel(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public void MIDIEnter(Signal n) {
            int layer = -n.Layer;

            if (_signals.ContainsKey(layer)) {
                if (n.Color.Lit) {
                    _signals[layer] = n;
                    
                    if (layer == _highest)
                        if (MIDIExit != null)
                            MIDIExit(n);
                
                } else {
                    _signals.Remove(layer);

                    if (layer == _highest) {
                        if (_signals.Count == 0) {
                            _highest = null;

                            if (MIDIExit != null)
                                MIDIExit(n);
                        
                        } else {
                            _highest = _signals.Keys[0];

                            if (MIDIExit != null)
                                MIDIExit(_signals[(int)_highest]);
                        }
                    }
                }
            
            } else {
                if (n.Color.Lit) {
                    _signals.Add(layer, n);

                    if (!_highest.HasValue || layer < _highest) {
                        _highest = layer;

                        if (MIDIExit != null)
                            MIDIExit(n);
                    }
                }
            }
        }
    }
}