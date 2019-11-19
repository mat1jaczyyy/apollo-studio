using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Fade: Device {
        public class FadeInfo {
            public Color Color;
            public double Time;
            
            public FadeType Type;

            public FadeInfo(Color color, double time) {
                Color = color;
                Time = time;
            }
        }

        List<Color> _colors = new List<Color>();
        List<double> _positions = new List<double>();
        List<FadeType> _types = new List<FadeType>();
        public List<FadeInfo> fade;

        public Color GetColor(int index) => _colors[index];
        public void SetColor(int index, Color color) {
            if (_colors[index] != color) {
                _colors[index] = color;
                Generate();

                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetColor(index, _colors[index]);
            }
        }

        public double GetPosition(int index) => _positions[index];
        public void SetPosition(int index, double position) {
            if (_positions[index] != position) {
                _positions[index] = position;
                Generate();
                
                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetPosition(index, _positions[index]);
            }
        }
        
        public FadeType GetFadeType(int index) => _types[index];
        public void SetFadeType(int index, FadeType type) {
            if (_types[index] != type) {
                _types[index] = type;
                Generate();
            }
        }
        
        ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        ConcurrentDictionary<Signal, List<Courier>> timers = new ConcurrentDictionary<Signal, List<Courier>>();
    
        static Dictionary<FadeType, Func<double, double, double, double>> TimeEasing = new Dictionary<FadeType, Func<double, double, double, double>>(){
            {FadeType.Linear, (double start, double end, double val) => val}, 
            {FadeType.Fast, (double start, double end, double val) => {
                double relativeVal = val - start;
                double duration = end - start;
                double proportion = relativeVal/duration;
                
                double factor = Math.Pow(proportion, 2);
                
                return start + duration*factor;
            }},
            {FadeType.Slow, (double start, double end, double val) => {
                double relativeVal = val - start;
                double duration = end - start;
                double proportion = relativeVal/duration;

                double factor = 1 - Math.Pow(1 - proportion, 2);
                
                return start + duration*factor;
            }},
            {FadeType.Sharp, (double start, double end, double val) => {
                double relativeVal = val - start;
                double duration = end - start;
                double proportion = relativeVal/duration;
                
                double factor;
                if(proportion < 0.5){
                    factor = Math.Pow(proportion - 0.5,2)*-2 + 0.5;
                    
                } else {
                    factor = Math.Pow(proportion - 0.5,2)*2 + 0.5;
                }
                
                return start + duration*factor;
            }},
            {FadeType.Smooth, (double start, double end, double val) => {
                double relativeVal = val - start;
                double duration = end - start;
                double proportion = relativeVal/duration;

                double factor;
                if(proportion < 0.5){
                    factor = Math.Pow(proportion,2)*2;
                } else {
                    factor = Math.Pow(proportion - 1,2)*-2 + 1;
                }
                
                
                return start + duration*factor;
            }},
            {FadeType.Hold, (double start, double end, double val) => start}
        };
    
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
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        void ModeChanged(bool value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    Generate();
                    
                    if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        FadePlaybackType _mode;
        public FadePlaybackType PlayMode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetPlaybackMode(PlayMode);
            }
        }

        public delegate void GeneratedEventHandler();
        public event GeneratedEventHandler Generated;

        void Generate() => Generate(Preferences.FadeSmoothness);

        void Generate(double smoothness) {
            if (_colors.Count < 2 || _positions.Count < 2) return;
            if (_types.Count < _colors.Count - 1) return;

            List<Color> _steps = new List<Color>();
            List<int> _counts = new List<int>();
            List<int> _cutoffs = new List<int>(){0};

            for (int i = 0; i < _colors.Count - 1; i++) {
                int max = new int[] {
                    Math.Abs(_colors[i].Red - _colors[i + 1].Red),
                    Math.Abs(_colors[i].Green - _colors[i + 1].Green),
                    Math.Abs(_colors[i].Blue - _colors[i + 1].Blue),
                    1
                }.Max();

                if(_types[i] == FadeType.Hold) {
                    _steps.Add(_colors[i]);
                    _counts.Add(1);
                    _cutoffs.Add(1 + _cutoffs.Last());
                } else{
                    for (double k = 0; k < max; k++) {
                        double factor = k/max;
                        _steps.Add(new Color(
                            (byte)(_colors[i].Red + (_colors[i + 1].Red - _colors[i].Red) * factor),
                            (byte)(_colors[i].Green + (_colors[i + 1].Green - _colors[i].Green) * factor),
                            (byte)(_colors[i].Blue + (_colors[i + 1].Blue - _colors[i].Blue) * factor)
                        ));
                    }
                    _counts.Add(max);
                    _cutoffs.Add(max + _cutoffs.Last());
                }
            }

            _steps.Add(_colors.Last());

            if (_steps.Last().Lit) {
                _steps.Add(new Color(0));
                _cutoffs[_cutoffs.Count - 1]++;
            }

            fade = new List<FadeInfo>() { new FadeInfo(_steps[0], 0){Type = _types[0]} };

            int j = 0;
            for (int i = 1; i < _steps.Count; i++) {
                if(_types[j] == FadeType.Hold) fade.Add(new FadeInfo(_steps[i - 1], GetPosition(j+1)*_time*_gate-0.001));
                
                if(_cutoffs[ j + 1] == i)j++;
                
                if (j < _colors.Count - 1) {
                    double prevTime = j != 0 ? _positions[j] * _time * _gate : 0;
                    double currTime = (_positions[j] + (_positions[j + 1] - _positions[j]) *(i - _cutoffs[j]) / _counts[j]) * _time * _gate;
                    double nextTime = _positions[j+1] * _time * _gate;
                    
                    double time = TimeEasing[_types[j]].Invoke(prevTime, nextTime, currTime);
                    
                    if (fade.Last().Time + smoothness < time) fade.Add(new FadeInfo(_steps[i], time));
                }
            }

            fade.Add(new FadeInfo(_steps.Last(), _time * _gate){Type = FadeType.Linear});
            
            Generated?.Invoke();
        }

        public int Count {
            get => _colors.Count;
        }

        public override Device Clone() => new Fade(_time.Clone(), _gate, PlayMode, (from i in _colors select i.Clone()).ToList(), _positions.ToList(), _types.ToList()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public void Insert(int index, Color color, double position, FadeType type) {
            _colors.Insert(index, color);
            _positions.Insert(index, position);
            _types.Insert(index, type);

            if (Viewer?.SpecificViewer != null) {
                FadeViewer SpecificViewer = ((FadeViewer)Viewer.SpecificViewer);
                SpecificViewer.Contents_Insert(index, _colors[index]);

                SpecificViewer.Expand(index);
            }

            Generate();
        }

        public void Remove(int index) {
            _colors.RemoveAt(index);
            _positions.RemoveAt(index);
            _types.RemoveAt(index);

            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).Contents_Remove(index);

            Generate();
        }

        public int? Expanded;

        public Fade(Time time = null, double gate = 1, FadePlaybackType playmode = FadePlaybackType.Mono, List<Color> colors = null, List<double> positions = null, List<FadeType> types = null, int? expanded = null) : base("fade") {
            Time = time?? new Time();
            Gate = gate;
            PlayMode = playmode;

            _colors = colors?? new List<Color>() {new Color(), new Color(0)};
            _positions = positions?? new List<double>() {0, 1};
            _types = types ?? new List<FadeType>() {FadeType.Linear};
            Expanded = expanded;

            Preferences.FadeSmoothnessChanged += Generate;

            if (Program.Project == null) Program.ProjectLoaded += Initialize;
            else Initialize();
        }

        void Initialize() {
            if (Disposed) return;

            Generate();
            Program.Project.BPMChanged += Generate;
        }

        void FireCourier(Signal n, double time) {
            Courier courier;

            timers[n].Add(courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            if (courier.Info.GetType() == typeof(Signal)) {
                Signal n = (Signal)courier.Info;

                lock (locker[n]) {
                    if (PlayMode == FadePlaybackType.Loop && !timers[n].Contains(courier)) return;

                    if (++buffer[n] == fade.Count - 1 && PlayMode == FadePlaybackType.Loop) {
                        Stop(n);
                        
                        for (int i = 1; i < fade.Count; i++)
                            FireCourier(n, fade[i].Time);
                    }
                    
                    if (buffer[n] < fade.Count) {
                        Signal m = n.Clone();
                        m.Color = fade[buffer[n]].Color.Clone();
                        InvokeExit(m);
                    }
                }
            }
        }

        void Stop(Signal n) {
            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (timers.ContainsKey(n))
                    for (int i = 0; i < timers[n].Count; i++)
                        timers[n][i].Dispose();
                
                if (PlayMode == FadePlaybackType.Loop && buffer.ContainsKey(n) && buffer[n] < fade.Count - 1) {
                    Signal m = n.Clone();
                    m.Color = fade.Last().Color.Clone();
                    InvokeExit(m);
                }

                timers[n] = new List<Courier>();
                buffer[n] = 0;
            }
        }

        public override void MIDIProcess(Signal n) {
            if (_colors.Count > 0) {
                bool lit = n.Color.Lit;
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if ((PlayMode == FadePlaybackType.Mono && lit) || PlayMode == FadePlaybackType.Loop) Stop(n);

                    if (lit) {
                        Signal m = n.Clone();
                        m.Color = fade[0].Color.Clone();
                        InvokeExit(m);
                        
                        for (int i = 1; i < fade.Count; i++)
                            FireCourier(n, fade[i].Time);
                    }
                }
            }
        }

        protected override void Stop() {
            foreach (List<Courier> i in timers.Values) {
                foreach (Courier j in i) j.Dispose();
                i.Clear();
            }
            timers.Clear();

            buffer.Clear();
            locker.Clear();
        }

        public override void Dispose() {
            if (Disposed) return;

            Generated = null;
            Preferences.FadeSmoothnessChanged -= Generate;

            if (Program.Project != null)
                Program.Project.BPMChanged -= Generate;

            Time.Dispose();
            base.Dispose();
        }
    }
}