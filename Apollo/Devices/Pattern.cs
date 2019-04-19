using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Devices {
    public class Pattern: Device {
        public static readonly new string DeviceIdentifier = "pattern";

        public PatternWindow Window;
        
        public override Device Clone() => new Pattern();

        public Pattern(): base(DeviceIdentifier) {
            
        }

        private void Tick(object info) {
            //if (info.GetType() == typeof()) {
                
            //}
        }

        public override void MIDIEnter(Signal n) {

        }

        public override void Dispose() {
            Window?.Close();
            Window = null;

            base.Dispose();
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Pattern(
                
            );
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
    }
}