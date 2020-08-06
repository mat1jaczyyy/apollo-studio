using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Rendering;
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
        
        ConcurrentDictionary<Signal, Color> buffer = new ConcurrentDictionary<Signal, Color>();
        
        public override void MIDIProcess(List<Signal> n) => InvokeExit(n.SelectMany(s => {
            Signal k = s.With(color: new Color(0));
            Color c = s.Color.Clone();
            
            if (s.Color.Lit)
                buffer[k] = c;
            
            if (s.Color.Lit != Release) {
                s.Color = buffer[k];
                
                if (!Infinite) Schedule(() => {
                    if (ReferenceEquals(buffer[k], c))
                        InvokeExit(new List<Signal>() {k});
                }, _time * _gate);
                
                return new [] {s};
            }
            
            return Enumerable.Empty<Signal>();
        }).ToList());

        protected override void Stopped() => buffer.Clear();

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            Time.Dispose();
            base.Dispose();
        }
        
        public class DurationUndoEntry: SimplePathUndoEntry<Hold, int> {
            protected override void Action(Hold item, int element) => item.Time.Free = element;
            
            public DurationUndoEntry(Hold hold, int u, int r)
            : base($"Hold Duration Changed to {r}ms", hold, u, r) {}
            
            DurationUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationModeUndoEntry: SimplePathUndoEntry<Hold, bool> {
            protected override void Action(Hold item, bool element) => item.Time.Mode = element;
            
            public DurationModeUndoEntry(Hold hold, bool u, bool r)
            : base($"Hold Duration Switched to {(r? "Steps" : "Free")}", hold, u, r) {}
            
            DurationModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationStepUndoEntry: SimplePathUndoEntry<Hold, int> {
            protected override void Action(Hold item, int element) => item.Time.Length.Step = element;
            
            public DurationStepUndoEntry(Hold hold, int u, int r)
            : base($"Hold Duration Changed to {Length.Steps[r]}", hold, u, r) {}
            
            DurationStepUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Hold, double> {
            protected override void Action(Hold item, double element) => item.Gate = element;
            
            public GateUndoEntry(Hold hold, double u, double r)
            : base($"Hold Gate Changed to {r}%", hold, u / 100, r / 100) {}
            
            GateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class InfiniteUndoEntry: SimplePathUndoEntry<Hold, bool> {
            protected override void Action(Hold item, bool element) => item.Infinite = element;
            
            public InfiniteUndoEntry(Hold hold, bool u, bool r)
            : base($"Hold Infinite Changed to {(r? "Enabled" : "Disabled")}", hold, u, r) {}
            
            InfiniteUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ReleaseUndoEntry: SimplePathUndoEntry<Hold, bool> {
            protected override void Action(Hold item, bool element) => item.Release = element;
            
            public ReleaseUndoEntry(Hold hold, bool u, bool r)
            : base($"Hold Release Changed to {(r? "Enabled" : "Disabled")}", hold, u, r) {}
            
            ReleaseUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}