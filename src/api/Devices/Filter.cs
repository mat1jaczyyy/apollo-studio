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
    public class Filter: Device {
        private bool[] _filter = new bool[128];

        public override Device Clone() {
            return new Filter(_filter);
        }

        public void Set(byte index, bool value) {
            if (0 <= index && index <= 127)
                _filter[index] = value;
        }

        public Filter() {}

        public Filter(bool[] init) {
            if (init.Length == 128)
                _filter = init;
        }

        public Filter(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Filter(bool[] init, Action<Signal> exit) {
            if (init.Length == 128)
                _filter = init;
            
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (_filter[n.Index])
                if (MIDIExit != null)
                    MIDIExit(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "filter") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            bool[] filter = new bool[128];
            for (int i = 0; i < 128; i++) {
                filter[i] = bool.Parse(data[i.ToString()].ToString());
            }

            return new Filter(filter);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("filter");

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < 128; i++) {
                            writer.WriteValue(_filter[i]);
                        }

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RequestSpecific(string jsonString) {
            throw new NotImplementedException();
        }
    }
}