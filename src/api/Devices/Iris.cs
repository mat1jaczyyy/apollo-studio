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
    public class Iris: Device {
        private int _rate = 200; // milliseconds
        public List<Color> Colors = new List<Color>();
        private Timer[] _timers = new Timer[128];
        private TimerCallback _timerexit;

        public int Rate {
            get {
                return _rate;
            }
            set {
                if (0 <= value)
                    _rate = value;
            }
        }

        public override Device Clone() {
            return new Iris(_rate, Colors);
        }

        public Iris() {
            _timerexit = new TimerCallback(Tick);
        }

        public Iris(int rate) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
        }

        public Iris(Color[] colors) {
            _timerexit = new TimerCallback(Tick);
            Colors = colors.ToList();
        }

        public Iris(List<Color> colors) {
            _timerexit = new TimerCallback(Tick);
            Colors = colors;
        }

        public Iris(int rate, Color[] colors) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            Colors = colors.ToList();
        }

        public Iris(int rate, List<Color> colors) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            Colors = colors;
        }

        public Iris(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            MIDIExit = exit;
        }

        public Iris(int rate, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            MIDIExit = exit;
        }

        public Iris(Color[] colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Colors = colors.ToList();
            MIDIExit = exit;
        }

        public Iris(List<Color> colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Colors = colors;
            MIDIExit = exit;
        }

        public Iris(int rate, Color[] colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            Colors = colors.ToList();
            MIDIExit = exit;
        }

        public Iris(int rate, List<Color> colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            Colors = colors;
            MIDIExit = exit;
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int))) {
                (byte index, int i) = ((byte, int))info;
                if (++i < Colors.Count) {
                    _timers[index] = new Timer(_timerexit, (index, i), _rate, System.Threading.Timeout.Infinite);
                    
                    Signal n = new Signal(index, Colors[i].Clone());

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
            if (Colors.Count > 0)
                if (n.Color.Lit) {
                    if (_timers[n.Index] != null)
                        _timers[n.Index].Dispose();

                    _timers[n.Index] = new Timer(_timerexit, (n.Index, 0), _rate, System.Threading.Timeout.Infinite);

                    n.Color = Colors[0].Clone();

                    if (MIDIExit != null)
                        MIDIExit(n);
                }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "iris") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<Color> init = new List<Color>();
            Dictionary<string, object> colors = JsonConvert.DeserializeObject<Dictionary<string, object>>(data["colors"].ToString());
            for (int i = 0; i < Convert.ToInt32(colors["count"]); i++) {
                init.Add(Color.Decode(colors[i.ToString()].ToString()));
            }

            return new Iris(Convert.ToInt32(data["rate"]), init);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("iris");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("rate");
                        writer.WriteValue(_rate);

                        writer.WritePropertyName("colors");
                        writer.WriteStartArray();

                            for (int i = 0; i < Colors.Count; i++) {
                                writer.WriteRawValue(Colors[i].Encode());
                            }

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RequestSpecific(string jsonString) {
            throw new NotImplementedException();
        }
    }
}