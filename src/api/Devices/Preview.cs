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
    public class Preview: Device {
        public static readonly new string DeviceIdentifier = "preview";

        private Pixel[] screen = new Pixel[128];

        public override Device Clone() {
            return new Preview();
        }

        public Preview(): base(DeviceIdentifier) {
            for (int i = 0; i < 128; i++)
                screen[i] = new Pixel() {MIDIExit = PreviewExit};
        }

        public void PreviewExit(Signal n) {
            Communication.UI.App(Request(new Dictionary<string, object>() {
                ["type"] = "signal",
                ["signal"] = n.Encode()
            }));
        }

        public override void MIDIEnter(Signal n) {
            Signal m = n.Clone();

            if (MIDIExit != null)
                MIDIExit(n);
            
            screen[m.Index].MIDIEnter(m);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            // Preview device has no data to parse

            return new Preview();
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();
                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RespondSpecific(string obj, string[] path, Dictionary<string, object> data) {
            if (path.Count() > 1) {
                return new BadRequestObjectResult("The Preview object has no members to forward to.");
            }

            switch (data["type"].ToString()) {
                case "signal":
                    Signal n = new Signal(Convert.ToByte(data["index"]), new Color((Convert.ToBoolean(data["press"]))? (byte)63: (byte)0));
                    MIDIEnter(n.Clone());
                    return new OkObjectResult(n.Encode());

                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}