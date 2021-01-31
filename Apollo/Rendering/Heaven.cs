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
        static ConcurrentQueue<List<Signal>> signalQueue = new();

        static SortedDictionary<long, List<Action>> jobs = new();
        static ConcurrentQueue<(long, Action)> jobQueue = new();

        static long prev, lastRender = -1, renderAt = -1;

        static long MSToTicks(double ms) => (long)(ms / 1000 * Stopwatch.Frequency);

        public static void MIDIEnter(List<Signal> n) {
            signalQueue.Enqueue(n);
            Wake();
        }
        
        public static void Schedule(Action job, double time) {
            jobQueue.Enqueue((MSToTicks(time), job));
            Wake();
        }

        static Task RenderThread;
        static bool Awake => RenderThread?.IsCompleted != false;

        public static double Time {
            get {
                if (RenderThread?.IsCompleted != false) prev = Program.TimeSpent.ElapsedTicks - 1;
                return prev * 1000.0 / Stopwatch.Frequency;
            }
        }

        static void Wake() {
            if (RenderThread?.IsCompleted == false) return;

            prev = Program.TimeSpent.ElapsedTicks - 1;

            RenderThread = Task.Run(() => {
                prev = Program.TimeSpent.ElapsedTicks - 1;
                
                while (renderAt >= 0 || jobQueue.Any() || jobs.Any() || signalQueue.Any()) {
                    while (jobQueue.TryDequeue(out (long Time, Action Job) task)) {
                        long target = task.Time;

                        if (!jobs.ContainsKey(target))
                            jobs[target] = new List<Action>();

                        jobs[target].Add(task.Job);
                    }

                    prev = Program.TimeSpent.ElapsedTicks;

                    foreach (long i in jobs.Keys.TakeWhile(i => i <= prev).ToList()) {
                        foreach (Action job in jobs[i])
                            job.Invoke();
                        
                        jobs.Remove(i);
                    }

                    bool changed = false;

                    while (signalQueue.TryDequeue(out List<Signal> n))
                        n.ForEach(i => {
                            if (i.Source != null) {
                                i.Source.Render(i);
                                changed = true;
                            }
                        });

                    if (changed && renderAt < 0)
                        renderAt = Math.Max(
                            prev + MSToTicks(250.0 / Preferences.FPSLimit),         // Buffer for collecting extra signals
                            lastRender + MSToTicks(1000.0 / Preferences.FPSLimit)   // FPS limit
                        );

                    else if (renderAt >= 0 && prev > renderAt) {  
                        Screen.Draw();
                        lastRender = prev;
                        renderAt = -1;
                    }
                }
            });
        }
    }
}