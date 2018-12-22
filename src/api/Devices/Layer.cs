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

        public override ObjectResult RespondSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != Identifier) return new BadRequestObjectResult("Incorrect recipient for message.");
            if (json["device"].ToString() != DeviceIdentifier) return new BadRequestObjectResult("Incorrect device recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    return new BadRequestObjectResult("The Layer object has no members to forward to.");
                
                case "target":
                    Target = Convert.ToInt32(data["value"]);
                    return new OkObjectResult(EncodeSpecific());
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}