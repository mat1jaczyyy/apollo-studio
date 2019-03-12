using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Apollo.Components;
using Apollo.Elements;

namespace Apollo.Devices {
    public class Layer: Device {
        public static readonly new string DeviceIdentifier = "layer";

        public int Target;

        public override Device Clone() {
            return new Layer(Target);
        }

        public Layer(int target = 0): base(DeviceIdentifier) {
            Target = target;
        }

        public override void MIDIEnter(Signal n) {
            n.Layer = Target;
            
            if (MIDIExit != null)
                MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Layer(Convert.ToInt32(data["target"]));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("target");
                        writer.WriteValue(Target);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RespondSpecific(string obj, string[] path, Dictionary<string, object> data) {
            if (path.Count() > 1) {
                return new BadRequestObjectResult("The Layer object has no members to forward to.");
            }

            switch (data["type"].ToString()) {
                case "target":
                    Target = Convert.ToInt32(data["value"]);
                    return new OkObjectResult(EncodeSpecific());
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}