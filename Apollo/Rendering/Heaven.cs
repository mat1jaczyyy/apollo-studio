using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Apollo.Structures;

namespace Apollo.Rendering {
    class Heaven: Renderer { 
        Dictionary<long, List<Signal>> signals = new Dictionary<long, List<Signal>>();

        bool rendering = false;
        long prev = -1;
        object locker = new object();

        public override void MIDIEnter(Signal n) {
            long target = prev + n.Delay + 1;

            if (!signals.ContainsKey(target))
                signals.Add(target, new List<Signal>());

            signals[target].Add(n);
        }

        public Heaven() {
            Task.Run(() => {
                Stopwatch time = new Stopwatch();
                time.Start();

                while (true) {
                    if (time.ElapsedMilliseconds <= prev) continue;

                    long last = prev;
                    prev = time.ElapsedMilliseconds;

                    for (long i = last + 1; i <= prev; i++) {
                        if (signals.ContainsKey(prev)) {
                            foreach (Signal n in signals[prev])
                                n.Source?.Render(n);

                            signals.Remove(prev);
                        }
                    }
                }
            });
        }
    }
}