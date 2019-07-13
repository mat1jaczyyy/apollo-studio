using System;
using System.Collections.Concurrent;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;

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
                    _time.Minimum = 10;
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

        ConcurrentQueue<Signal> buffer = new ConcurrentQueue<Signal>();
        object locker = new object();
        ConcurrentHashSet<Courier> timers = new ConcurrentHashSet<Courier>();

        public override Device Clone() => new Delay(_time.Clone(), _gate) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Delay(Time time = null, double gate = 1): base("delay") {
            Time = time?? new Time();
            Gate = gate;
        }

        void Tick(object sender, EventArgs e) {
            if (Disposed) return;
            
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            lock (locker) {
                if (buffer.TryDequeue(out Signal n))
                    MIDIExit?.Invoke(n);
            }
        }

        public override void MIDIProcess(Signal n) {
            lock (locker) {
                buffer.Enqueue(n.Clone());

                Courier courier = new Courier() {
                    AutoReset = false,
                    Interval = _time * _gate,
                };
                courier.Elapsed += Tick;
                courier.Start();
            }
        }

        protected override void Stop() {
            foreach (Courier i in timers) i.Dispose();
            timers.Clear();
            
            buffer.Clear();
            locker = new object();
        }

        public override void Dispose() {
            Stop();
            
            Time.Dispose();
            base.Dispose();
        }
    }
}