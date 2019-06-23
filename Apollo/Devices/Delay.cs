using System;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Delay: Device {
        public static readonly new string DeviceIdentifier = "delay";

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
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        private void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetMode(value);
        }

        private void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((DelayViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        public override Device Clone() => new Delay(_time.Clone(), _gate) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Delay(Time time = null, decimal gate = 1): base(DeviceIdentifier) {
            Time = time?? new Time();
            Gate = gate;
        }

        private void Tick(object sender, EventArgs e) {
            if (Disposed) return;
            
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIProcess(Signal n) {
            Courier courier = new Courier() {
                Info = n.Clone(),
                AutoReset = false,
                Interval = (double)(_time * _gate),
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        public override void Dispose() {
            Time.Dispose();
            base.Dispose();
        }
    }
}