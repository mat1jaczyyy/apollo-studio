using System;
using System.Collections.Concurrent;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Hold: Device {
        Time _time;
        public Time Time {
            get => _time;
            set {
                if (_time != null) {
                    _time.FreeChanged -= FreeChanged;
                    _time.ModeChanged -= ModeChanged;
                    _time.StepChanged -= StepChanged;
                }

                _time = value;

                if (_time != null) {
                    _time.Minimum = 10;
                    _time.Maximum = 30000;

                    _time.FreeChanged += FreeChanged;
                    _time.ModeChanged += ModeChanged;
                    _time.StepChanged += StepChanged;
                }
            }
        }

        void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                _infinite = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetInfinite(Infinite);
            }
        }

        bool _release;
        public bool Release {
            get => _release;
            set {
                _release = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetRelease(Release);
            }
        }

        ConcurrentDictionary<Signal, Color> releasebuffer = new ConcurrentDictionary<Signal, Color>();
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();

        ConcurrentQueue<Signal> buffer = new ConcurrentQueue<Signal>();
        object queuelocker = new object();
        ConcurrentHashSet<Courier> timers = new ConcurrentHashSet<Courier>();
        ConcurrentDictionary<Signal, int> ignores = new ConcurrentDictionary<Signal, int>();

        public override Device Clone() => new Hold(_time.Clone(), _gate, Infinite, Release) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Hold(Time time = null, double gate = 1, bool infinite = false, bool release = false): base("hold") {
            Time = time?? new Time();
            Gate = gate;
            Infinite = infinite;
            Release = release;
        }

        void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            lock (queuelocker) {
                if (buffer.TryDequeue(out Signal n) && ignores[n]-- <= 0)
                    InvokeExit(n.Clone());
            }
        }

        public override void MIDIProcess(Signal n) {
            Color color = n.Color.Clone();
            n.Color = new Color(0);
            
            if (!locker.ContainsKey(n)) locker[n] = new object();
            
            lock (locker[n]) {
                if (color.Lit && Release) releasebuffer[n] = color;

                if (color.Lit != Release) {
                    if (!Infinite)
                        lock (queuelocker) {
                            Signal m = n.Clone();

                            if (!ignores.ContainsKey(m))
                                ignores[m] = -1;
                            
                            ignores[m]++;

                            buffer.Enqueue(m);

                            Courier courier;
                            timers.Add(courier = new Courier() {
                                AutoReset = false,
                                Interval = _time * _gate,
                            });
                            courier.Elapsed += Tick;
                            courier.Start();
                        }

                    if (Release) {
                        if (!releasebuffer.ContainsKey(n)) return;

                        color = releasebuffer[n];
                        releasebuffer.TryRemove(n, out Color _);
                    }

                    n.Color = color;
                    InvokeExit(n);
                }
            }
        }

        protected override void Stop() {
            foreach (Courier i in timers) i.Dispose();
            timers.Clear();
            
            buffer.Clear();
            queuelocker = new object();

            releasebuffer.Clear();
            locker.Clear();
            ignores.Clear();
        }

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            Time.Dispose();
            base.Dispose();
        }
        
        public class DurationUndoEntry: PathUndoEntry<Hold> {
            int u, r;
            
            protected override void UndoPath(params Hold[] items) => items[0].Time.Free = u;
            protected override void RedoPath(params Hold[] items) => items[0].Time.Free = r;
            
            public DurationUndoEntry(Hold hold, int u, int r)
            : base($"Hold Duration Changed to {r}ms", hold) {
                this.u = u;
                this.r = r;
            }
        }
        
        public class DurationModeUndoEntry: PathUndoEntry<Hold> {
            bool u, r;
            
            protected override void UndoPath(params Hold[] items) => items[0].Time.Mode = u;
            protected override void RedoPath(params Hold[] items) => items[0].Time.Mode = r;
            
            public DurationModeUndoEntry(Hold hold, bool u, bool r)
            : base($"Hold Duration Switched to {(r? "Steps" : "Free")}", hold) {
                this.u = u;
                this.r = r;
            }
        }
        
        public class DurationStepUndoEntry: PathUndoEntry<Hold> {
            int u, r;
            
            protected override void UndoPath(params Hold[] items) => items[0].Time.Length.Step = u;
            protected override void RedoPath(params Hold[] items) => items[0].Time.Length.Step = r;
            
            public DurationStepUndoEntry(Hold hold, int u, int r)
            : base($"Hold Duration Changed to {Length.Steps[r]}", hold) {
                this.u = u;
                this.r = r;
            }
        }
        
        public class GateUndoEntry: PathUndoEntry<Hold> {
            double u, r;
            
            protected override void UndoPath(params Hold[] items) => items[0].Gate = u;
            protected override void RedoPath(params Hold[] items) => items[0].Gate = r;
            
            public GateUndoEntry(Hold hold, double u, double r)
            : base($"Hold Gate Changed to {r}%", hold) {
                this.u = u;
                this.r = r;
            }
        }
        
        public class InfiniteUndoEntry: PathUndoEntry<Hold> {
            bool u, r;
            
            protected override void UndoPath(params Hold[] items) => items[0].Infinite = u;
            protected override void RedoPath(params Hold[] items) => items[0].Infinite = r;
            
            public InfiniteUndoEntry(Hold hold, bool u, bool r)
            : base($"Hold Infinite Changed to {(r? "Enabled" : "Disabled")}", hold) {
                this.u = u;
                this.r = r;
            }
        }
        
        public class ReleaseUndoEntry: PathUndoEntry<Hold> {
            bool u, r;
            
            protected override void UndoPath(params Hold[] items) => items[0].Release = u;
            protected override void RedoPath(params Hold[] items) => items[0].Release = r;
            
            public ReleaseUndoEntry(Hold hold, bool u, bool r)
            : base($"Hold Release Changed to {(r? "Enabled" : "Disabled")}", hold) {
                this.u = u;
                this.r = r;
            }
        }
    }
}