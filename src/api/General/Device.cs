using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public abstract class Device: IRequest, IResponse {
        public static readonly string Identifier = "device";
        public readonly string DeviceIdentifier;

        public IDeviceParent Parent = null;
        public int? ParentIndex;
        public Action<Signal> MIDIExit = null;

        public abstract Device Clone();
        
        protected Device(string device) {
            DeviceIdentifier = device;
        }

        public abstract void MIDIEnter(Signal n);

        public static Device Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            foreach (Type device in (from type in Assembly.GetExecutingAssembly().GetTypes() where (type.Namespace.StartsWith("api.Devices") && !type.Namespace.StartsWith("api.Devices.Device")) select type)) {
                var parsed = device.GetMethod("DecodeSpecific").Invoke(null, new object[] {json["data"].ToString()});
                if (parsed != null) return (Device)parsed;
            }
            return null;
        }

        public abstract string EncodeSpecific();
        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteRawValue(EncodeSpecific());

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
        
        public string Request(Dictionary<string, object> data, List<string> path = null) {
            if (path == null) path = new List<string>();
            path.Insert(0, Identifier);

            if (ParentIndex != null)
                path[0] += $":{ParentIndex}";

            return Parent.Request(data, path);
        }

        public abstract ObjectResult RespondSpecific(string obj, string[] path, Dictionary<string, object> data);
        public ObjectResult Respond(string obj, string[] path, Dictionary<string, object> data) {
            if (path[0] != Identifier) return new BadRequestObjectResult("Incorrect recipient for message.");
            return RespondSpecific(obj, path, data);
        }
    }
}