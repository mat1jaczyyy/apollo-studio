using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

using Apollo.Components;
using Apollo.Core;

namespace Apollo.Elements {
    public abstract class Device {
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
            
            foreach (Type device in (from type in Assembly.GetExecutingAssembly().GetTypes() where (type.Namespace.StartsWith("Apollo.Devices") && !type.Namespace.StartsWith("Apollo.Devices.Device")) select type)) {
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
    }
}