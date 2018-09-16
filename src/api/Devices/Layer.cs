using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Layer: Device {
        public int Target = 0;

        public override Device Clone() {
            return new Layer(Target);
        }

        public Layer() {}

        public Layer(int target) {
            Target = target;
        }

        public Layer(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Layer(int target, Action<Signal> exit) {
            Target = target;
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            n.Layer = Target;
            
            if (MIDIExit != null)
                MIDIExit(n);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("layer");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("target");
                        writer.WriteValue(Target);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}