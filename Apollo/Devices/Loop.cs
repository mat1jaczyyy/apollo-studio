using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Loop: Device {
        
        struct LoopCourierInfo{
            public int count, amount;
            public Signal signal;
            
            public LoopCourierInfo(int c, int a, Signal s){
                count = c;
                amount = a;
                signal = s;
            }
        }
        Time _duration;
        public Time Duration {
            get => _duration;
            set {
                if (_duration != null) {
                    _duration.FreeChanged -= FreeChanged;
                    _duration.ModeChanged -= ModeChanged;
                    _duration.StepChanged -= StepChanged;
                }

                _duration = value;

                if (_duration != null) {
                    _duration.Minimum = 10;
                    _duration.Maximum = 30000;

                    _duration.FreeChanged += FreeChanged;
                    _duration.ModeChanged += ModeChanged;
                    _duration.StepChanged += StepChanged;
                }
            }
        }
        
        int _amount = 0;
        public int Amount {
            get => _amount;
            set {
                if (0 <= value && value <= 100 && _amount != value) {
                    _amount = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetAmount(Amount);
                }
            }
        }
        
        ConcurrentDictionary<Signal, Courier> timers = new ConcurrentDictionary<Signal, Courier>();

        void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((LoopViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }
        
        public Loop(Time duration = null, int amount = 0) : base("loop"){
            Duration = duration?? new Time();
            Amount = amount;
        }
        
        public override Device Clone() => new Loop(Duration, Amount);
        
        public override void MIDIProcess(Signal n){
            Courier courier;
            timers[n] = courier = new Courier(){
                Info = new LoopCourierInfo(0, Amount, n),
                Interval = Duration,
                AutoReset = true
            };
            
            courier.Elapsed += Tick;
            courier.Start();
            
            Signal m = n.Clone();
            InvokeExit(m);
        }
        
        void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            LoopCourierInfo info = (LoopCourierInfo) courier.Info;
            
            int count = info.count + 1;
            
            if(count > info.amount){
                courier.Stop();
                courier.Elapsed -= Tick;
                courier.Dispose();
                return;
            }
            
            info.count = count;
            
            timers[info.signal].Info = info;
                        
            Signal m = info.signal.Clone();
            InvokeExit(m);
        }
    }
}