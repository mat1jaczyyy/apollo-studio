using System.Collections.Generic;
using System.IO;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Rendering;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Delay: Device {
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
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        public override Device Clone() => new Delay(_time.Clone(), _gate) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Delay(Time time = null, double gate = 1): base("delay") {
            Time = time?? new Time();
            Gate = gate;
        }

        public override void MIDIProcess(List<Signal> n)
            => Schedule(() => InvokeExit(n), Heaven.Time + _time * _gate);

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            Time.Dispose();
            base.Dispose();
        }
        
        public class DurationUndoEntry: SimplePathUndoEntry<Delay, int> {
            protected override void Action(Delay item, int element) => item.Time.Free = element;
            
            public DurationUndoEntry(Delay delay, int u, int r)
            : base($"Delay Duration Changed to {r}ms", delay, u, r) {}
            
            DurationUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationModeUndoEntry: SimplePathUndoEntry<Delay, bool> {
            protected override void Action(Delay item, bool element) => item.Time.Mode = element;
            
            public DurationModeUndoEntry(Delay delay, bool u, bool r)
            : base($"Delay Duration Switched to {(r? "Steps" : "Free")}", delay, u, r) {}
            
            DurationModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationStepUndoEntry: SimplePathUndoEntry<Delay, int> {
            protected override void Action(Delay item, int element) => item.Time.Length.Step = element;
            
            public DurationStepUndoEntry(Delay delay, int u, int r)
            : base($"Delay Duration Changed to {Length.Steps[r]}", delay, u, r) {}
            
            DurationStepUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Delay, double> {
            protected override void Action(Delay item, double element) => item.Gate = element;
            
            public GateUndoEntry(Delay delay, double u, double r)
            : base($"Delay Gate Changed to {r}%", delay, u / 100, r / 100) {}
            
            GateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}