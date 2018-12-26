using System;
using System.Collections.Concurrent;
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
        
        private int[] _indexes = new int[128];
        private object[] locker = new object[128];

        private List<Timer>[] _timers = new List<Timer>[128];
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

            if (colors == null) colors = new List<Color>() {new Color(63), new Color(31), new Color(15)};

            Rate = rate;
            Colors = colors;

            for (int i = 0; i < 128; i++)
                locker[i] = new object();
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int))) {
                (byte index, int layer) = ((byte, int))info;

                lock (locker[index]) {
                    int color = ++_indexes[index];

                    if (color < Colors.Count) {
                        if (MIDIExit != null)
                            MIDIExit(new Signal(index, Colors[color].Clone(), layer));
                    } else { // TODO: Only if last color is not 0?
                        if (MIDIExit != null)
                            MIDIExit(new Signal(index, new Color(0), layer));
                    }
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (Colors.Count > 0 && n.Color.Lit) {
                if (_timers[n.Index] != null) 
                    for (int i = 0; i < _timers[n.Index].Count; i++) 
                        _timers[n.Index][i].Dispose();
                
                _timers[n.Index] = new List<Timer>();
                _indexes[n.Index] = 0;

                n.Color = Colors[0].Clone();

                if (MIDIExit != null)
                    MIDIExit(n);
                
                for (int i = 1; i <= Colors.Count; i++) {
                    _timers[n.Index].Add(new Timer(_timerexit, (n.Index, n.Layer), _rate * i, Timeout.Infinite));
                }
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