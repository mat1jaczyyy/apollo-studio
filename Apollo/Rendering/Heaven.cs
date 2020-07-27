using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Apollo.Core;
using Apollo.Structures;

namespace Apollo.Rendering {
    public static class Heaven { 
        static Dictionary<long, List<Signal>> signals = new Dictionary<long, List<Signal>>();
        static ConcurrentQueue<List<Signal>> queue = new ConcurrentQueue<List<Signal>>();

        static long prev;
        static object locker = new object();

        public static void MIDIEnter(IEnumerable<Signal> n) {
            queue.Enqueue(n.ToList());
            Wake();
        }

        static void Process(List<Signal> n, long start) {
            foreach (Signal i in n) {
                long target = start + i.Delay + 1;

                if (!signals.ContainsKey(target))
                    signals.Add(target, new List<Signal>());

                signals[target].Add(i);
            }
        }

        static Task RenderThread; 

        static void Wake() {
            if (RenderThread?.IsCompleted == false) return;

            RenderThread = Task.Run(() => {
                prev = Program.TimeSpent.ElapsedMilliseconds - 1;
                
                while (queue.Any() || signals.Any()) {
                    if (Program.TimeSpent.ElapsedMilliseconds <= prev) continue; // TODO Heaven cap fps for non-CFW

                    while (queue.TryDequeue(out List<Signal> n))
                        Process(n, prev);

                    bool changed = false;

                    while (true) {
                        long last = prev;
                        prev = Program.TimeSpent.ElapsedMilliseconds;

                        long diff = prev - last;

                        Task.Run(() => {
                            if (diff >= 8)
                                Console.WriteLine($"[Heaven] Long wait: {diff}");
                        });

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
                        for (long i = prev + 1; i < Program.TimeSpent.ElapsedMilliseconds; i++)
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