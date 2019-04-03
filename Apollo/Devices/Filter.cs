using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Filter: Device {
        public static readonly new string DeviceIdentifier = "filter";

        private bool[] _filter;

        public override Device Clone() => new Filter(_filter);

        public bool this[int index] {
            get => _filter[index];
            set {
                if (1 <= index && index <= 99)
                    _filter[index] = value;
            }
        }
        
        public Filter(bool[] init = null): base(DeviceIdentifier) {
            if (init == null || init.Length != 100) init = new bool[100];
            _filter = init;
        }

        public override void MIDIEnter(Signal n) {
            if (_filter[n.Index])
                MIDIExit?.Invoke(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            List<object> data = JsonConvert.DeserializeObject<List<object>>(json["data"].ToString());
            
            bool[] filter = new bool[100];

            for (int i = 1; i <= 99; i++)
                filter[i] = Convert.ToBoolean(data[i].ToString());

            return new Filter(filter);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 1; i <= 99; i++)
                            writer.WriteValue(_filter[i]);

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}