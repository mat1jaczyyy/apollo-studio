using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Apollo.Structures;

namespace Apollo.Rendering {
    class Heaven: Renderer { 
        Dictionary<long, List<Signal>> signals = new Dictionary<long, List<Signal>>();
        ConcurrentQueue<List<Signal>> queue = new ConcurrentQueue<List<Signal>>();

        long prev = -1;
        object locker = new object();

        public override void MIDIEnter(IEnumerable<Signal> n) => queue.Enqueue(n.ToList());

        void Process(List<Signal> n, long start) {
            foreach (Signal i in n) {
                long target = start + i.Delay + 1;

                if (!signals.ContainsKey(target))
                    signals.Add(target, new List<Signal>());

                signals[target].Add(i);
            }
        }

        public Heaven() {
            Task.Run(() => {
                Stopwatch time = new Stopwatch();
                time.Start();

                while (true) {
                    if (time.ElapsedMilliseconds <= prev) continue; // TODO Heaven cap fps for non-CFW

                    while (queue.TryDequeue(out List<Signal> n))
                        Process(n, prev);

                    bool changed = false;

                    while (true) {
                        long last = prev;
                        prev = time.ElapsedMilliseconds;

                        for (long i = last + 1; i <= prev; i++) {
                            if (signals.ContainsKey(i)) {
                                foreach (Signal n in signals[i]) {
                                    if (n.Validate(out List<Signal> extra)) {
                                        changed = true;
                                        n.Source?.Render(n);
                                    }

                                    if (extra != null) Process(extra, i);
                                }

                                signals.Remove(i);
                            }
                        }

                        // Frame skipping
                        // TODO Heaven Make toggleable in Preferences
                        for (long i = prev + 1; i < time.ElapsedMilliseconds; i++)
                            if (signals.ContainsKey(i))
                                continue;

                        if (changed)
                            Screen.Draw();

                        break;
                    }
                }
            });
        }
    }
}