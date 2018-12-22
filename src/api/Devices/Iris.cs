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
        public static readonly new string DeviceIdentifier = "iris";

        private int _rate; // milliseconds
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

        public Iris(int rate = 200, List<Color> colors = null): base(DeviceIdentifier) {
            _timerexit = new TimerCallback(Tick);

            if (colors == null) colors = new List<Color>() {new Color(63), new Color(31), new Color(15), new Color(0)};

            Rate = rate;
            Colors = colors;
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
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> colors = JsonConvert.DeserializeObject<List<object>>(data["colors"].ToString());
            List<Color> init = new List<Color>();
            foreach (object color in colors) {
                init.Add(Color.Decode(color.ToString()));
            }

            return new Iris(Convert.ToInt32(data["rate"]), init);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

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

        public override ObjectResult RespondSpecific(string jsonString) {
            throw new NotImplementedException();
        }
    }
}