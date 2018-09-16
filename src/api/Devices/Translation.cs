using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Translation: Device {
        private int _offset = 0;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-127 <= value && value <= 127)
                    _offset = value;
            }
        }

        public override Device Clone() {
            return new Translation(_offset);
        }

        public Translation() {}

        public Translation(int offset) {
            Offset = offset;
        }

        public Translation(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Translation(int offset, Action<Signal> exit) {
            Offset = offset;
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            int result = n.Index + _offset;

            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.Index = (byte)result;

            if (MIDIExit != null)
                MIDIExit(n);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("paint");

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