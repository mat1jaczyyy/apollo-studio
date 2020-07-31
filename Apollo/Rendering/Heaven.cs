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

        static Dictionary<long, List<Func<IEnumerable<Signal>>>> scheduled = new Dictionary<long, List<Func<IEnumerable<Signal>>>>();
        static ConcurrentQueue<(long, Func<IEnumerable<Signal>>)> squeue = new ConcurrentQueue<(long, Func<IEnumerable<Signal>>)>();

        static long prev, sprev;
        static object locker = new object();

        public static void MIDIEnter(IEnumerable<Signal> n) {
            queue.Enqueue(n.ToList());
            Wake();
        }
        
        public static void Schedule(Func<IEnumerable<Signal>> job, double time) {
            squeue.Enqueue(((long)time, job));
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
                sprev = Program.TimeSpent.ElapsedMilliseconds - 1;
                prev = Program.TimeSpent.ElapsedMilliseconds - 1;
                
                while (squeue.Any() || scheduled.Any() || queue.Any() || signals.Any()) {
                    if (Program.TimeSpent.ElapsedMilliseconds <= prev) continue; // TODO Heaven cap fps for non-CFW

                    while (squeue.TryDequeue(out (long a, Func<IEnumerable<Signal>> b) a)) {
                        long target = sprev + a.a + 1;

                        if (!scheduled.ContainsKey(target))
                            scheduled[target] = new List<Func<IEnumerable<Signal>>>();

                        scheduled[target].Add(a.b);
                    }

                    long last = sprev;
                    sprev = Program.TimeSpent.ElapsedMilliseconds;

                    for (long i = last + 1; i <= sprev; i++) {
                        if (scheduled.ContainsKey(i)) {
                            foreach (Func<IEnumerable<Signal>> job in scheduled[i])
                                Heaven.MIDIEnter(job.Invoke());
                            
                            scheduled.Remove(i);
                        }
                    }

                    while (queue.TryDequeue(out List<Signal> n))
                        Process(n, prev);

                    bool changed = false;

                    last = prev;
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

                    if (changed)
                        Screen.Draw();
                }
            });
        }
    }
}