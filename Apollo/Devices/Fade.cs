using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Fade: Device {
        public static readonly new string DeviceIdentifier = "fade";

        private List<Color> _colors = new List<Color>();
        private List<decimal> _positions = new List<decimal>();
        private List<Color> _steps = new List<Color>();
        private List<int> _counts = new List<int>();
        private List<int> _cutoffs = new List<int>();
        
        private ConcurrentDictionary<Signal, int> _indexes = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();

        public Color GetColor(int index) => _colors[index];
        public void SetColor(int index, Color color) {
            _colors[index] = color;
            Generate();
        }

        public decimal GetPosition(int index) => _positions[index];
        public void SetPosition(int index, decimal position) {
            _positions[index] = position;
            Generate();
        }

        private ConcurrentDictionary<Signal, List<Courier>> _timers = new ConcurrentDictionary<Signal, List<Courier>>();

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;
        private decimal _gate;

        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4)
                    _gate = value;
            }
        }

        public delegate void GeneratedEventHandler();
        public event GeneratedEventHandler Generated;

        private void Generate(double amount = 0) {
            if (amount < 0.01 || 1 > amount) amount = Preferences.FadeSmoothness;

            _steps = new List<Color>();
            _counts = new List<int>();
            _cutoffs = new List<int>() {0};

            for (int i = 0; i < _colors.Count - 1; i++) {
                int max = new int[] {
                    Math.Abs(_colors[i].Red - _colors[i + 1].Red),
                    Math.Abs(_colors[i].Green - _colors[i + 1].Green),
                    Math.Abs(_colors[i].Blue - _colors[i + 1].Blue),
                    1
                }.Max();

                int count = 0;
                double tick = 1 - amount;
                for (int j = 0; j < max; j++) {
                    tick += amount;

                    if (tick >= 1) {
                        tick += -1;

                        _steps.Add(new Color(
                            (byte)(_colors[i].Red + (_colors[i + 1].Red - _colors[i].Red) * j / max),
                            (byte)(_colors[i].Green + (_colors[i + 1].Green - _colors[i].Green) * j / max),
                            (byte)(_colors[i].Blue + (_colors[i + 1].Blue - _colors[i].Blue) * j / max)
                        ));

                        count++;
                    }
                }

                _counts.Add(count);
                _cutoffs.Add(count + _cutoffs.Last());
            }

            _steps.Add(_colors.Last());

            if (_steps.Last().Lit) {
                _steps.Add(new Color(0));
                _cutoffs[_cutoffs.Count - 1]++;
            }

            Generated?.Invoke();
        }

        public int Count {
            get => _colors.Count;
        }

        public override Device Clone() => new Fade(Mode, Length, _time, _gate, _colors, _positions);

        public void Insert(int index, Color color, decimal position) {
            _colors.Insert(index, color);
            _positions.Insert(index, position);
            Generate();
        }

        public void Remove(int index) {
            _colors.RemoveAt(index);
            _positions.RemoveAt(index);
            Generate();
        }

        public Fade(bool mode = false, Length length = null, int time = 1000, decimal gate = 1, List<Color> colors = null, List<decimal> positions = null): base(DeviceIdentifier) {
            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Gate = gate;

            _colors = colors?? new List<Color>() {new Color(63), new Color(0)};
            _positions = positions?? new List<decimal>() {0, 1};

            Preferences.FadeSmoothnessChanged += Generate;
            Generate();
        }

        private void FireCourier(Signal n, int time) {
            Courier courier;

            _timers[n].Add(courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            if (courier.Info.GetType() == typeof(Signal)) {
                Signal n = (Signal)courier.Info;

                lock (locker[n]) {
                    if (++_indexes[n] < _steps.Count) {
                        Signal m = n.Clone();
                        m.Color = _steps[_indexes[n]].Clone();
                        MIDIExit?.Invoke(m);
                    }
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (_colors.Count > 0 && n.Color.Lit) {
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if (_timers.ContainsKey(n))
                        for (int i = 0; i < _timers[n].Count; i++)
                            _timers[n][i].Dispose();

                    _timers[n] = new List<Courier>();
                    _indexes[n] = 0;
                    
                    Signal m = n.Clone();
                    m.Color = _steps[0].Clone();
                    MIDIExit?.Invoke(m);
                    
                    int j = 0;
                    for (int i = 1; i < _steps.Count; i++) {
                        if (_cutoffs[j + 1] == i) j++;

                        if (j < _colors.Count - 1)
                            FireCourier(n, (int)((_positions[j] + (_positions[j + 1] - _positions[j]) * (i - _cutoffs[j]) / _counts[j]) * (Mode? (int)Length : _time) * _gate));
                    }

                    FireCourier(n, (int)((Mode? (int)Length : _time) * _gate));
                }
            }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> _colors = JsonConvert.DeserializeObject<List<object>>(data["colors"].ToString());
            List<Color> initC = new List<Color>();

            foreach (object color in _colors)
                initC.Add(Color.Decode(color.ToString()));

            List<object> _positions = JsonConvert.DeserializeObject<List<object>>(data["positions"].ToString());
            List<decimal> initP = new List<decimal>();

            foreach (object position in _positions)
                initP.Add(decimal.Parse(position.ToString()));

            return new Fade(
                Convert.ToBoolean(data["mode"]),
                Length.Decode(data["length"].ToString()),
                Convert.ToInt32(data["time"]),
                Convert.ToDecimal(data["gate"]),
                initC,
                initP
            );
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("mode");
                        writer.WriteValue(Mode);

                        writer.WritePropertyName("length");
                        writer.WriteRawValue(Length.Encode());

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("gate");
                        writer.WriteValue(_gate);

                        writer.WritePropertyName("colors");
                        writer.WriteStartArray();

                            for (int i = 0; i < _colors.Count; i++)
                                writer.WriteRawValue(_colors[i].Encode());

                        writer.WriteEndArray();

                        writer.WritePropertyName("positions");
                        writer.WriteStartArray();

                            for (int i = 0; i < _positions.Count; i++)
                                writer.WriteValue(_positions[i]);

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}