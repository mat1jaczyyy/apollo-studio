using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Fade: Device {
        private int _time = 1000; // milliseconds
        private List<Color> _colors = new List<Color>();
        private List<Decimal> _positions = new List<Decimal>();
        private List<Color> _steps = new List<Color>();
        private List<int> _counts = new List<int>();
        private List<int> _cutoffs = new List<int>();
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
            _counts = new List<int>();
            _cutoffs = new List<int>();

            for (int i = 0; i < _colors.Count - 1; i++) {
                _counts.Add(new int[] {
                    Math.Abs(_colors[i].Red - _colors[i + 1].Red),
                    Math.Abs(_colors[i].Green - _colors[i + 1].Green),
                    Math.Abs(_colors[i].Blue - _colors[i + 1].Blue)
                }.Max());

                for (int j = 0; j < _counts.Last(); j++) {
                    _steps.Add(new Color(
                        (byte)(_colors[i].Red + (_colors[i + 1].Red - _colors[i].Red) * j / _counts.Last()),
                        (byte)(_colors[i].Green + (_colors[i + 1].Green - _colors[i].Green) * j / _counts.Last()),
                        (byte)(_colors[i].Blue + (_colors[i + 1].Blue - _colors[i].Blue) * j / _counts.Last())
                    ));
                }

                if (i > 0) {
                    _cutoffs.Add(_counts.Last() + _cutoffs.Last());
                } else {
                    _cutoffs.Add(_counts.Last());
                }
            }

            _steps.Add(_colors.Last());

            if (_steps.Last().Lit) {
                _steps.Add(new Color(0));
                _cutoffs[_cutoffs.Count - 1]++;
            }

            //_counts.Add(_counts.Last());
        }

        public override Device Clone() {
            return new Fade(_time, _colors, _positions);
        }

        public Fade(int time, List<Color> colors, List<Decimal> positions) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            _colors = colors;
            _positions = positions;
            Generate();
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int, int))) {
                (byte index, int i, int j) = ((byte, int, int))info;
                
                if (_cutoffs[i] == ++j)
                    i++;
                
                if (i < _colors.Count - 1)
                    _timers[index] = new Timer(_timerexit, (index, i, j), (int)((_positions[i + 1] - _positions[i]) * _time / _counts[i]), System.Threading.Timeout.Infinite);

                if (MIDIExit != null)
                    MIDIExit(new Signal(index, _steps[j].Clone()));
            }
        }

        public override void MIDIEnter(Signal n) {
            if (_colors.Count > 0)
                if (n.Color.Lit) {
                    if (_timers[n.Index] != null)
                        _timers[n.Index].Dispose();

                    _timers[n.Index] = new Timer(_timerexit, (n.Index, 0, 0), (int)((_positions[1] - _positions[0]) * _time / _counts[0]), System.Threading.Timeout.Infinite);

                    n.Color = _steps[0].Clone();

                    if (MIDIExit != null)
                        MIDIExit(n);
                }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "fade") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> colors = JsonConvert.DeserializeObject<List<object>>(data["colors"].ToString());
            List<Color> initC = new List<Color>();
            foreach (object color in colors) {
                initC.Add(Color.Decode(color.ToString()));
            }

            List<object> positions = JsonConvert.DeserializeObject<List<object>>(data["positions"].ToString());
            List<Decimal> initP = new List<Decimal>();
            foreach (object position in positions) {
                initP.Add(Decimal.Parse(position.ToString()));
            }

            return new Fade(Convert.ToInt32(data["time"]), initC, initP);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("fade");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("colors");

                        writer.WriteStartArray();

                            for (int i = 0; i < _colors.Count; i++) {
                                writer.WriteRawValue(_colors[i].Encode());
                            }

                        writer.WriteEndArray();

                        writer.WritePropertyName("positions");
                        writer.WriteStartArray();

                            for (int i = 0; i < _positions.Count; i++) {
                                writer.WriteValue(_positions[i]);
                            }

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RespondSpecific(string jsonString) {
            throw new NotImplementedException();
        }
    }
}