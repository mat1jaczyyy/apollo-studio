using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Loop: Device {
        Time _rate;
        public Time Rate {
            get => _rate;
            set {
                if (_rate != null) {
                    _rate.FreeChanged -= FreeChanged;
                    _rate.ModeChanged -= ModeChanged;
                    _rate.StepChanged -= StepChanged;
                }

                _rate = value;

                if (_rate != null) {
                    _rate.Minimum = 10;
                    _rate.Maximum = 30000;

                    _rate.FreeChanged += FreeChanged;
                    _rate.ModeChanged += ModeChanged;
                    _rate.StepChanged += StepChanged;
                }
            }
        }

        void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetRateValue(value);
        }

        void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetRateStep(value);
        }
        
        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }
        
        int _repeats;
        public int Repeats {
            get => _repeats;
            set {
                if (1 <= value && value <= 128 && _repeats != value) {
                    _repeats = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetRepeats(Repeats);
                }
            }
        }
        
        bool _hold;
        public bool Hold {
            get => _hold;
            set {
                _hold = value;

                if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetHold(Hold);
            }
        }
        
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        ConcurrentDictionary<Signal, List<Courier>> timers = new ConcurrentDictionary<Signal, List<Courier>>();
        
        public override Device Clone() => new Loop(Rate.Clone(), Gate, Repeats, Hold);
        
        public Loop(Time rate = null, double gate = 1, int repeats = 2, bool hold = false): base("loop") {
            Rate = rate?? new Time();
            Gate = gate;
            Repeats = repeats;
            Hold = hold;
        }
        
        void FireCourier(Signal n, Signal m, double time)
            => timers[n].Add(new Courier<Signal>(time, m.Clone(), Tick));

        void Start(Signal n, Signal m, int count) {
            if (!timers.ContainsKey(n)) 
                timers[n] = new List<Courier>();

            InvokeExit(m.Clone());

            if (!Hold || m.Color.Lit)
                for (int i = 1; i < count; i++)
                    FireCourier(n, m, i * _rate * _gate);
        }
        
        public override void MIDIProcess(Signal n) {
            Signal m = n.Clone();

            if (Hold) {
                n.Color = new Color();
                
                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if (timers.ContainsKey(n))
                        for (int i = 0; i < timers[n].Count; i++)
                            timers[n][i].Dispose();
                    
                    timers[n] = new List<Courier>();

                    Start(n, m, 2);
                }
            
            } else Start(n, m, Repeats);
        }
        
        void Tick(Courier<Signal> sender, Signal n) {
            if (Disposed) return;
            
            Signal m = n.Clone();

            if (Hold) {
                n.Color = new Color();

                lock (locker[n]) {
                    FireCourier(n, m, _rate * _gate);
                    timers[n].Remove(sender);
                    
                    InvokeExit(m.With(m.Index, new Color(0)));
                    InvokeExit(m);
                }
            
            } else {
                timers[n].Remove(sender);
                InvokeExit(m);
            }
        }

        protected override void Stop() {
            foreach (List<Courier> i in timers.Values) {
                foreach (Courier j in i) j.Dispose();
                i.Clear();
            }
            timers.Clear();
        }
        
        public override void Dispose() {
            if (Disposed) return;
            
            Stop();

            base.Dispose();
        }
        
        public class RateUndoEntry: SimplePathUndoEntry<Loop, int> {
            protected override void Action(Loop item, int element) => item.Rate.Free = element;
            
            public RateUndoEntry(Loop loop, int u, int r)
            : base($"Loop Rate Changed to {r}ms", loop, u, r) {}
            
            RateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class RateModeUndoEntry: SimplePathUndoEntry<Loop, bool> {
            protected override void Action(Loop item, bool element) => item.Rate.Mode = element;
            
            public RateModeUndoEntry(Loop loop, bool u, bool r)
            : base($"Loop Rate Switched to {(r? "Steps" : "Free")}", loop, u, r) {}
            
            RateModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class RateStepUndoEntry: SimplePathUndoEntry<Loop, int> {
            protected override void Action(Loop item, int element) => item.Rate.Length.Step = element;
            
            public RateStepUndoEntry(Loop loop, int u, int r)
            : base($"Loop Rate Changed to {Length.Steps[r]}", loop, u, r) {}
            
            RateStepUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class HoldUndoEntry: SimplePathUndoEntry<Loop, bool> {
            protected override void Action(Loop item, bool element) => item.Hold = element;
            
            public HoldUndoEntry(Loop loop, bool u, bool r)
            : base($"Loop Hold Changed to {(r? "Enabled" : "Disabled")}", loop, u, r) {}
            
            HoldUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Loop, double> {
            protected override void Action(Loop item, double element) => item.Gate = element;
            
            public GateUndoEntry(Loop loop, double u, double r)
            : base($"Loop Gate Changed to {r}%", loop, u / 100, r / 100) {}
            
            GateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class RepeatsUndoEntry: SimplePathUndoEntry<Loop, int> {
            protected override void Action(Loop item, int element) => item.Repeats = element;
            
            public RepeatsUndoEntry(Loop loop, int u, int r)
            : base($"Loop Repeats Changed to {r}", loop, u, r) {}
            
            RepeatsUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}