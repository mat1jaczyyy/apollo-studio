using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Duplication: Device {
        private List<int> _offsets = new List<int>();

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

        public Duplication() {}

        public Duplication(int[] offsets) {
            Offsets = offsets.ToList();
        }

        public Duplication(List<int> offsets) {
            Offsets = offsets;
        }

        public Duplication(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Duplication(int[] offsets, Action<Signal> exit) {
            Offsets = offsets.ToList();
            MIDIExit = exit;
        }

        public Duplication(List<int> offsets, Action<Signal> exit) {
            Offsets = offsets;
            MIDIExit = exit;
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

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("duplication");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("count");
                        writer.WriteValue(_offsets.Count);

                        for (int i = 0; i < _offsets.Count; i++) {
                            writer.WritePropertyName(i.ToString());
                            writer.WriteValue(_offsets[i]);
                        }

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}