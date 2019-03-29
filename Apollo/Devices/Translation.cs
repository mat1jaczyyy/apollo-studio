using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Translation: Device {
        public static readonly new string DeviceIdentifier = "translation";

        private int _offset;

        public int Offset {
            get => _offset;
            set {
                if (-99 <= value && value <= 99)
                    _offset = value;
            }
        }

        public override Device Clone() => new Translation(_offset);

        public Translation(int offset = 0): base(DeviceIdentifier) => Offset = offset;

        public override void MIDIEnter(Signal n) {
            int result = n.Index + _offset;

            if (result < 0) result = 0;
            if (result > 99) result = 99;

            n.Index = (byte)result;

            MIDIExit?.Invoke(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Translation(Convert.ToInt32(data["offset"]));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("offset");
                        writer.WriteValue(_offset);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}