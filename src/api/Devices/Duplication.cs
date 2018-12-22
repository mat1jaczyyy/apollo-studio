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
    public class Duplication: Device {
        public static readonly new string DeviceIdentifier = "duplication";

        private List<int> _offsets;

        public List<int> Offsets {
            get {
                return _offsets;
            }
            set {
                foreach (int offset in value) {
                    if (offset <= -127 || 127 <= offset) return;
                }
                _offsets = value;
            }
        }

        public override Device Clone() {
            return new Duplication(_offsets);
        }

        public void Insert(int index, int offset) {
            if (offset <= -127 || 127 <= offset)
                _offsets.Insert(index, offset);
        }

        public void Add(int offset) {
            if (offset <= -127 || 127 <= offset)
                _offsets.Add(offset);
        }

        public void Remove(int index) {
            _offsets.RemoveAt(index);
        }

        public Duplication(List<int> offsets = null): base(DeviceIdentifier) {
            if (offsets == null) offsets = new List<int>();
            Offsets = offsets;
        }

        public override void MIDIEnter(Signal n) {
            if (MIDIExit != null)
                MIDIExit(n);
            
            foreach (int offset in _offsets) {
                Signal m = n.Clone();

                int result = m.Index + offset;
                
                if (result < 0) result = 0;
                if (result > 127) result = 127;

                m.Index = (byte)result;

                if (MIDIExit != null)
                    MIDIExit(m);
            }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            List<object> data = JsonConvert.DeserializeObject<List<object>>(json["data"].ToString());
            
            List<int> offsets = new List<int>();
            foreach (object offset in offsets) {
                offsets.Add(Convert.ToInt32(offset));
            }
            return new Duplication(offsets);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < _offsets.Count; i++) {
                            writer.WriteValue(_offsets[i]);
                        }

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RespondSpecific(string jsonString) {
            throw new NotImplementedException();
        }
    }
}