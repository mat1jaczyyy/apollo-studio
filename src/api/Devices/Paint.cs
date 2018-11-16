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
        private Color _color = new Color(63);

        public override Device Clone() {
            return new Paint(_color);
        }

        public Paint() {}

        public Paint(Color color) {
            _color = color.Clone();
        }
        
        public Paint(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Paint(Color color, Action<Signal> exit) {
            _color = color.Clone();
            MIDIExit = exit;
        }

        private byte Scale(byte value, byte high, byte low) {
            if (value == 0)
                return 0;
            
            return (byte)(((high - low) * value) / 63 + low);
        }

        public override void MIDIEnter(Signal n) {
            n.Color = _color.Clone();

            if (MIDIExit != null)
                MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "paint") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Paint(Color.Decode(data["color"].ToString()));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("paint");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("color");
                        writer.WriteRawValue(_color.Encode());

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