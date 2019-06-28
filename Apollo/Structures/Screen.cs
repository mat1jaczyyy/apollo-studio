using System;
using System.Collections.Generic;

using Apollo.Enums;

namespace Apollo.Structures {
    public class Screen {
        private class Pixel {
            public Action<Signal> Exit = null;
            
            private SortedList<int, Signal> _signals = new SortedList<int, Signal>() {
                [10000] = new Signal(null, 11, new Color(0))
            };
            
            private Color state = new Color(0);

            private object locker = new object();

            public Pixel() {}

            public Color GetColor() {
                Color ret = new Color(0);

                for (int i = 0; i < _signals.Count; i++) {
                    Signal signal = _signals.Values[i];

                    if (signal.BlendingMode == BlendingType.Mask) break;

                    ret.Mix(signal.Color, (i == 0)? false : (_signals.Values[i - 1].BlendingMode == BlendingType.Multiply));

                    if (signal.BlendingMode == BlendingType.Normal) break;
                }

                return ret;
            }

            public void MIDIEnter(Signal n) {
                lock (locker) {
                    int layer = -n.Layer;

                    if (n.Color.Lit) _signals[layer] = n.Clone();
                    else if (_signals.ContainsKey(layer)) _signals.Remove(layer);
                    else return;

                    Color newState = GetColor();
                    
                    if (newState != state) {
                        Signal m = n.Clone();
                        m.Color = state = newState;
                        Exit?.Invoke(m);
                    }
                }
            }
        }

        public Action<Signal> ScreenExit;

        private Pixel[] _screen = new Pixel[100];

        public Screen() {
            for (int i = 0; i < 100; i++)
                _screen[i] = new Pixel() { Exit = (n) => ScreenExit?.Invoke(n) };
        }

        public Color GetColor(int index) => _screen[index].GetColor();

        public void MIDIEnter(Signal n) => _screen[n.Index].MIDIEnter(n);
    }
}