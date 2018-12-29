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
    public class Infinity: Device {
        public static readonly new string DeviceIdentifier = "infinity";

        public override Device Clone() {
            return new Infinity();
        }

        public Infinity(): base(DeviceIdentifier) {}

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit)
                if (MIDIExit != null)
                    MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;
            
            // Infinity device has no data to parse

            return new Infinity();
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
            throw new NotImplementedException();
        }
    }
}