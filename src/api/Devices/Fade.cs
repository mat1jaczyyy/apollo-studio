using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Fade: Device {
        private int _time = 1000; // milliseconds
        private List<Color> _colors = new List<Color>();
        private List<double> _positions = new List<double>();
        private List<Color> _steps = new List<Color>();
        private Timer[] _timers = new Timer[128];
        private TimerCallback _timerexit;

        public int Time {
            get {
                return _time;
            }
            set {
                if (0 <= value)
                    _time = value;
            }
        }

        private void Generate() {
            _steps = new List<Color>();
            _positions = new List<double>();

            for (int i = 0; i < _colors.Count - 1; i++) {
                _positions.Add((double)i / _colors.Count);
                
                int n = new int[] {
                    Math.Abs(_colors[i].Red - _colors[i + 1].Red),
                    Math.Abs(_colors[i].Green - _colors[i + 1].Green),
                    Math.Abs(_colors[i].Blue - _colors[i + 1].Blue)
                }.Max();

                for (int j = 0; j < n; j++) {
                    _steps.Add(new Color(
                        (byte)(_colors[i].Red + (_colors[i + 1].Red - _colors[i].Red) * j / n),
                        (byte)(_colors[i].Green + (_colors[i + 1].Green - _colors[i].Green) * j / n),
                        (byte)(_colors[i].Blue + (_colors[i + 1].Blue - _colors[i].Blue) * j / n)
                    ));
                }
            }

            _steps.Add(_colors.Last());
        }

        public override Device Clone() {
            return new Fade(_time, _colors);
        }

        public Fade() {
            _timerexit = new TimerCallback(Tick);
            Generate();
        }

        public Fade(int time) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            Generate();
        }

        public Fade(Color[] colors) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors.ToList();
            Generate();
        }

        public Fade(List<Color> colors) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors;
            Generate();
        }

        public Fade(int time, Color[] colors) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            _colors = colors.ToList();
            Generate();
        }

        public Fade(int time, List<Color> colors) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            _colors = colors;
            Generate();
        }

        public Fade(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            MIDIExit = exit;
            Generate();
        }

        public Fade(int time, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            MIDIExit = exit;
            Generate();
        }

        public Fade(Color[] colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors.ToList();
            MIDIExit = exit;
            Generate();
        }

        public Fade(List<Color> colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors;
            MIDIExit = exit;
            Generate();
        }

        public Fade(int time, Color[] colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            _colors = colors.ToList();
            MIDIExit = exit;
            Generate();
        }

        public Fade(int time, List<Color> colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            _colors = colors;
            MIDIExit = exit;
            Generate();
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int))) {
                (byte index, int i) = ((byte, int))info;
                if (++i < _colors.Count) {
                    _timers[index] = new Timer(_timerexit, (index, i), _time, System.Threading.Timeout.Infinite);
                    
                    Signal n = new Signal(index, _colors[i].Clone());

                    if (MIDIExit != null)
                        MIDIExit(n);
                
                } else {
                    Signal n = new Signal(index, new Color(0));

                    if (MIDIExit != null)
                        MIDIExit(n);
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (_colors.Count > 0)
                if (n.Color.Lit) {
                    if (_timers[n.Index] != null)
                        _timers[n.Index].Dispose();

                    _timers[n.Index] = new Timer(_timerexit, (n.Index, 0), _time, System.Threading.Timeout.Infinite);

                    n.Color = _colors[0].Clone();

                    if (MIDIExit != null)
                        MIDIExit(n);
                }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "Fade") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<Color> init = new List<Color>();
            Dictionary<string, object> colors = JsonConvert.DeserializeObject<Dictionary<string, object>>(data["colors"].ToString());
            for (int i = 0; i < int.Parse(colors["count"].ToString()); i++) {
                init.Add(Color.Decode(colors[i.ToString()].ToString()));
            }

            return new Fade(int.Parse(data["time"].ToString()), init);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("Fade");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("colors");
                        writer.WriteStartObject();

                            writer.WritePropertyName("count");
                            writer.WriteValue(_colors.Count);

                            for (int i = 0; i < _colors.Count; i++) {
                                writer.WritePropertyName(i.ToString());
                                writer.WriteRawValue(_colors[i].Encode());
                            }

                        writer.WriteEndObject();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}