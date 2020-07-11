using System;
using System.Collections.Generic;

using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Rendering {
    public class Screen: IDisposable {
        class Pixel {
            byte Index;
            
            SortedList<int, Signal> _signals = new SortedList<int, Signal>();
            
            Color state = new Color(0);

            object locker = new object();

            public void Clear() {
                lock (locker) {
                    _signals.Clear();
                    _signals.Add(10000, new Signal(null, null, color: new Color(0), layer: -100));
                }
            }

            public Pixel(int index) {
                Index = (byte)index;
                Clear();
            }

            public Color GetColor() {
                Color ret = new Color(0);

                for (int i = 0; i < _signals.Count; i++) {
                    Signal signal = _signals.Values[i];
                    if (signal.BlendingMode != BlendingType.Normal && ((i == _signals.Count - 1)? true : signal.Layer - _signals.Values[i + 1].Layer > signal.BlendingRange))
                        continue;

                    if (signal.BlendingMode == BlendingType.Mask) break;

                    ret.Mix(signal.Color, (i == 0)? false : (_signals.Values[i - 1].BlendingMode == BlendingType.Multiply && _signals.Values[i - 1].Layer - signal.Layer <= _signals.Values[i - 1].BlendingRange));

                    if (signal.BlendingMode == BlendingType.Normal) break;
                }

                return ret;
            }

            public void MIDIEnter(Signal n) {
                lock (locker) {
                    if (n.Index != Index) return;

                    int layer = -n.Layer;

                    if (n.Color.Lit) _signals[layer] = n.Clone();
                    else if (_signals.ContainsKey(layer)) _signals.Remove(layer);
                    else return;
                }
            }
        }

        public Action<List<Signal>> ScreenExit;

        Pixel[] _screen = new Pixel[101];

        public void Clear() {
            foreach (Pixel pixel in _screen)
                pixel.Clear();
        }

        Color[] snapshot = new Color[101];

        void Snapshot() {
            List<Signal> ret = new List<Signal>();

            for (int i = 0; i < 101; i++) {
                Color n = _screen[i].GetColor();

                if (snapshot[i] != n) {
                    ret.Add(new Signal(null, null, (byte)i, n.Clone()));
                    snapshot[i] = n;
                }
            }

            if (ret.Count > 0)
                ScreenExit?.Invoke(ret);
        }

        delegate void DrawingHandler();
        static event DrawingHandler Drawing;

        public static void Draw() => Drawing?.Invoke();

        public Screen() {
            for (int i = 0; i < 101; i++) {
                snapshot[i] = new Color(0);
                _screen[i] = new Pixel(i);
            }
            
            Drawing += Snapshot;
        }

        public Color GetColor(int index) => _screen[index].GetColor();

        public void MIDIEnter(Signal n) => _screen[n.Index].MIDIEnter(n);

        public void Dispose() {
            Drawing -= Snapshot;
        }
    }
}