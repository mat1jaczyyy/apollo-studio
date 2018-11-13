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
    public class Paint: Device {
        private Color _high = new Color(63), _low = new Color(0);

        public override Device Clone() {
            return new Paint(_high, _low);
        }

        public Paint() {}

        public Paint(Color color) {
            _high = color.Clone();
            _low = color.Clone();
        }

        public Paint(Color high, Color low) {
            _high = high.Clone();
            _low = low.Clone();
        }
        
        public Paint(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Paint(Color color, Action<Signal> exit) {
            _high = color.Clone();
            _low = color.Clone();
            MIDIExit = exit;
        }

        public Paint(Color high, Color low, Action<Signal> exit) {
            _high = high.Clone();
            _low = low.Clone();
            MIDIExit = exit;
        }

        private byte Scale(byte value, byte high, byte low) {
            if (value == 0)
                return 0;
            
            return (byte)(((high - low) * value) / 63 + low);
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit) {
                n.Color.Red = Scale(n.Color.Red, _high.Red, _low.Red);
                n.Color.Green = Scale(n.Color.Green, _high.Green, _high.Green);
                n.Color.Blue = Scale(n.Color.Blue, _high.Blue, _high.Blue);
            }

            if (MIDIExit != null)
                MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "paint") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Paint(Color.Decode(data["high"].ToString()), Color.Decode(data["low"].ToString()));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("paint");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("high");
                        writer.WriteRawValue(_high.Encode());

                        writer.WritePropertyName("low");
                        writer.WriteRawValue(_low.Encode());

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