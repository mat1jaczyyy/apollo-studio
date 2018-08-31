using System;
using System.Collections.Generic;
using System.Linq;

namespace api {
    public class Pixel {
        private SortedList<int, Signal> _signals = new SortedList<int, Signal>();
        private int _highest = -1;
        public Action<Signal> MIDIExit = null;

        public Pixel() {}

        public Pixel(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public void MIDIEnter(Signal n) {
            if (_signals.ContainsKey(n.Layer)) {
                if (n.Color.Lit) {
                    _signals[n.Layer] = n;
                    
                    if (n.Layer == _highest)
                        if (MIDIExit != null)
                            MIDIExit(n);
                
                } else {
                    _signals.Remove(n.Layer);

                    if (n.Layer == _highest) {
                        if (_signals.Count == 0) {
                            _highest = -1;

                            if (MIDIExit != null)
                                MIDIExit(n);
                        
                        } else {
                            _highest = _signals.Keys[0];

                            if (MIDIExit != null)
                                MIDIExit(_signals[_highest]);
                        }
                    }
                }
            
            } else {
                if (n.Color.Lit) {
                    _signals.Add(n.Layer, n);

                    if (_highest == -1 || n.Layer < _highest) {
                        _highest = n.Layer;

                        if (MIDIExit != null)
                            MIDIExit(n);
                    }
                }
            }
        }
    }
}