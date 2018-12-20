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
        private Pixel[] screen = new Pixel[128];

        public override Device Clone() {
            return new Preview();
        }

        public Preview() {
            for (int i = 0; i < 128; i++)
                screen[i] = new Pixel(PreviewExit);
        }

        public Preview(Action<Signal> exit) {
            for (int i = 0; i < 128; i++)
                screen[i] = new Pixel(PreviewExit);
            
            MIDIExit = exit;
        }

        public void PreviewExit(Signal n) {
            // TODO: Request app drawing
        }

        public override void MIDIEnter(Signal n) {
            Signal m = n.Clone();

            if (MIDIExit != null)
                MIDIExit(n);
            
            screen[m.Index].MIDIEnter(m);
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
                
                case "signal":
                    Signal n = new Signal(Convert.ToByte(data["index"]), new Color(63));
                    MIDIEnter(n.Clone());
                    return new OkObjectResult(n.Encode());

                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}