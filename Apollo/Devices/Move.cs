using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Move: Device {
        public static readonly new string DeviceIdentifier = "move";

        public Offset Offset;
        public bool Loop;

        public override Device Clone() => new Move(Offset);

        public Move(Offset offset = null, bool loop = false): base(DeviceIdentifier) {
            if (offset == null) offset = new Offset();
            
            Offset = offset;
            Loop = loop;
        }

        public override void MIDIEnter(Signal n) {
            int x = n.Index % 10 + Offset.X;
            int y = n.Index / 10 + Offset.Y;

            if (Loop) {
                x = (x + 10) % 10;
                y = (y + 10) % 10;
            }

            int result = y * 10 + x;
                
            if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 99 && result != 9 && result != 90) {
                n.Index = (byte)result;
                MIDIExit?.Invoke(n);
            }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Move(
                Offset.Decode(data["offset"].ToString()),
                Convert.ToBoolean(data["loop"])
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

                        writer.WritePropertyName("offset");
                        writer.WriteRawValue(Offset.Encode());

                        writer.WritePropertyName("loop");
                        writer.WriteValue(Loop);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}