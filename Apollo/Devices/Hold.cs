using System;
using System.Collections.Concurrent;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Hold: Device {
        private Time _time;
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

        private void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        private void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetMode(value);
        }

        private void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        private bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                _infinite = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetInfinite(Infinite);
            }
        }

        private bool _release;
        public bool Release {
            get => _release;
            set {
                _release = value;
                
                if (Viewer?.SpecificViewer != null) ((HoldViewer)Viewer.SpecificViewer).SetRelease(Release);
            }
        }

        private ConcurrentDictionary<Signal, Color> buffer = new ConcurrentDictionary<Signal, Color>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();

        public override Device Clone() => new Hold(_time.Clone(), _gate, Infinite, Release) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Hold(Time time = null, decimal gate = 1, bool infinite = false, bool release = false): base("hold") {
            Time = time?? new Time();
            Gate = gate;
            Infinite = infinite;
            Release = release;
        }

        private void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIProcess(Signal n) {
            Color color = n.Color.Clone();
            n.Color = new Color(0);
            
            if (!locker.ContainsKey(n)) locker[n] = new object();
            
            lock (locker[n]) {
                if (color.Lit && Release) buffer[n] = color;

                if (color.Lit != Release) {
                    if (!Infinite) {
                        Courier courier = new Courier() {
                            Info = n.Clone(),
                            AutoReset = false,
                            Interval = (double)(_time * _gate),
                        };
                        courier.Elapsed += Tick;
                        courier.Start();
                    }

                    if (Release) {
                        color = buffer[n];
                        buffer.TryRemove(n, out Color _);
                    }

                    n.Color = color;
                    MIDIExit?.Invoke(n);
                }
            }
        }

        public override void Dispose() {
            Time.Dispose();
            base.Dispose();
        }
    }
}