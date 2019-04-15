using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Fade: Device {
        public static readonly new string DeviceIdentifier = "fade";

        private int _time; // milliseconds
        private List<Color> _colors = new List<Color>();
        private List<Decimal> _positions = new List<Decimal>();
        private List<Color> _steps = new List<Color>();
        private List<int> _counts = new List<int>();
        private List<int> _cutoffs = new List<int>();
        
        private int[] _indexes = new int[100];
        private object[] locker = new object[100];

        private List<Timer>[] _timers = new List<Timer>[100];
        private TimerCallback _timerexit;

        public Color GetColor(int index) => _colors[index];
        public void SetColor(int index, Color color) {
            _colors[index] = color;
            Generate();
        }

        public Decimal GetPosition(int index) => _positions[index];
        public void SetPosition(int index, Decimal position) {
            _positions[index] = position;
            Generate();
        }

        public int Time {
            get => _time;
            set {
                if (0 <= value) _time = value;
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

        public override Device Clone() => new Fade(_time, _colors, _positions);

        public void Insert(int index, Color color, Decimal position) {
            _colors.Insert(index, color);
            _positions.Insert(index, position);
            Generate();
        }

        public void Remove(int index) {
            _colors.RemoveAt(index);
            _positions.RemoveAt(index);
            Generate();
        }

        public Fade(int time = 1000, List<Color> colors = null, List<Decimal> positions = null): base(DeviceIdentifier) {
            _timerexit = new TimerCallback(Tick);

            if (colors == null) colors = new List<Color>() {new Color(63), new Color(0)};
            if (positions == null) positions = new List<Decimal>() {0, 1};
            
            Time = time;
            _colors = colors;
            _positions = positions;

            for (int i = 0; i < 100; i++)
                locker[i] = new object();

            Preferences.FadeSmoothnessChanged += Generate;
            Generate();
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int))) {
                (byte index, int layer) = ((byte, int))info;

                lock (locker[index]) {
                    int color = ++_indexes[index];

                    if (color < _steps.Count)
                        MIDIExit?.Invoke(new Signal(Track.Get(this).Launchpad, index, _steps[color].Clone(), layer));
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (_colors.Count > 0 && n.Color.Lit) {
                if (_timers[n.Index] != null) 
                    for (int i = 0; i < _timers[n.Index].Count; i++) 
                        _timers[n.Index][i].Dispose();

                _timers[n.Index] = new List<Timer>();
                _indexes[n.Index] = 0;

                n.Color = _steps[0].Clone();

                MIDIExit?.Invoke(n);

                int j = 0;
                for (int i = 1; i < _steps.Count; i++) {
                    if (_cutoffs[j + 1] == i) j++;

                    if (j < _colors.Count - 1)
                        _timers[n.Index].Add(new Timer(
                            _timerexit,
                            (n.Index, n.Layer),
                            (int)((_positions[j] + (_positions[j + 1] - _positions[j]) * (i - _cutoffs[j]) / _counts[j]) * _time),
                            Timeout.Infinite
                        ));
                }

                _timers[n.Index].Add(new Timer(_timerexit, (n.Index, n.Layer), _time, Timeout.Infinite));
            }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> _colors = JsonConvert.DeserializeObject<List<object>>(data["_colors"].ToString());
            List<Color> initC = new List<Color>();

            foreach (object color in _colors)
                initC.Add(Color.Decode(color.ToString()));

            List<object> _positions = JsonConvert.DeserializeObject<List<object>>(data["_positions"].ToString());
            List<Decimal> initP = new List<Decimal>();

            foreach (object position in _positions)
                initP.Add(Decimal.Parse(position.ToString()));

            return new Fade(
                Convert.ToInt32(data["time"]),
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

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("_colors");

                        writer.WriteStartArray();

                            for (int i = 0; i < _colors.Count; i++)
                                writer.WriteRawValue(_colors[i].Encode());

                        writer.WriteEndArray();

                        writer.WritePropertyName("_positions");
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