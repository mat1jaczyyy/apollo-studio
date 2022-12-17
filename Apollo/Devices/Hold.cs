using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
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
                    _time.Minimum = 1;
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

        HoldType _holdmode;
        public HoldType HoldMode {
            get => _holdmode;
            set {
                _holdmode = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetHoldMode(HoldMode);

                Stop();
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

        bool ActualRelease => HoldMode == HoldType.Minimum? false : Release;

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { _time.Clone(), _gate, HoldMode, Release };

        public Hold(Time time = null, double gate = 1, HoldType holdmode = HoldType.Trigger, bool release = false): base("hold") {
            Time = time?? new Time();
            Gate = gate;
            HoldMode = holdmode;
            Release = release;
        }
        
        ConcurrentDictionary<Signal, Color> buffer = new();
        ConcurrentDictionary<Signal, int> minimum = new();
        
        public override void MIDIProcess(List<Signal> n) => InvokeExit(n.SelectMany(s => {
            Signal k = s.With(color: new Color(0));
            
            if (s.Color.Lit)
                buffer[k] = s.Color;
            
            if (s.Color.Lit != ActualRelease) {
                if (!buffer.ContainsKey(k))  // happens if receives 0 as first input (issue #428)
                    return new Signal[] {};

                s.Color = buffer[k];
                
                if (HoldMode != HoldType.Infinite) {
                    if (HoldMode == HoldType.Minimum) minimum[k] = 0;

                    Schedule(() => {
                        if (ReferenceEquals(buffer[k], s.Color)) {
                            if (HoldMode == HoldType.Minimum && minimum[k] == 0) {
                                minimum[k] = 2;
                                return;
                            }

                            InvokeExit(new List<Signal>() {k.Clone()});
                        }
                    }, Heaven.Time + _time * _gate);
                }
                
                return new [] {s};

            } else if (HoldMode == HoldType.Minimum) {
                if (minimum[k] == 0) minimum[k] = 1;
                else if (minimum[k] == 2)
                    InvokeExit(new List<Signal>() {k.Clone()});
            }
            
            return Enumerable.Empty<Signal>();
        }).Select(i => i.Clone()).ToList());

        protected override void Stopped() {
            buffer.Clear();
            minimum.Clear();
        }

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
        
        public class HoldModeUndoEntry: EnumSimplePathUndoEntry<Hold, HoldType> {
            protected override void Action(Hold item, HoldType element) => item.HoldMode = element;
            
            public HoldModeUndoEntry(Hold hold, HoldType u, HoldType r, IEnumerable source)
            : base("Hold Mode", hold, u, r, source) {}
            
            HoldModeUndoEntry(BinaryReader reader, int version)
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