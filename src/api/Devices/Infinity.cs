using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Infinity: Device {
        public override Device Clone() {
            return new Infinity();
        }

        public Infinity() {}

        public Infinity(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit)
                if (MIDIExit != null)
                    MIDIExit(n);
        }

         public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("infinity");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();
                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}