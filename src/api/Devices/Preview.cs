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
    public class Preview: Device {
        public override Device Clone() {
            return new Preview();
        }

        public Preview() {}

        public Preview(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "preview") return null;

            // Preview device has no data to parse

            return new Preview();
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("preview");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();
                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RequestSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != "device") return new BadRequestObjectResult("Incorrect recipient for message.");
            if (json["device"].ToString() != "preview") return new BadRequestObjectResult("Incorrect device recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    return new BadRequestObjectResult("The Preview object has no members to forward to.");
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}