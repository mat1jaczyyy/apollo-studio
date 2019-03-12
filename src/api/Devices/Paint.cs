using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api.Components;
using api.Elements;

namespace api.Devices {
    public class Paint: Device {
        public static readonly new string DeviceIdentifier = "paint";

        private Color _color;

        public override Device Clone() {
            return new Paint(_color);
        }

        public Paint(Color color = null): base(DeviceIdentifier) {
            _color = (color == null)? new Color(63) : color.Clone();
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit)
                n.Color = _color.Clone();

            if (MIDIExit != null)
                MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Paint(Color.Decode(data["color"].ToString()));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("color");
                        writer.WriteRawValue(_color.Encode());

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RespondSpecific(string obj, string[] path, Dictionary<string, object> data) {
            if (path.Count() > 1) {
                return new BadRequestObjectResult("The Paint object has no members to forward to.");
            }

            switch (data["type"].ToString()) {
                
                case "color":
                    _color.Red = Convert.ToByte(data["red"]);
                    _color.Green = Convert.ToByte(data["green"]);
                    _color.Blue = Convert.ToByte(data["blue"]);
                    return new OkObjectResult(EncodeSpecific());
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}