using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Flip: Device {
        public static readonly new string DeviceIdentifier = "flip";

        public enum FlipType {
            Horizontal,
            Vertical,
            Diagonal1,
            Diagonal2
        }

        public FlipType Mode;

        public override Device Clone() => new Flip(Mode);

        public Flip(FlipType mode = FlipType.Horizontal): base(DeviceIdentifier) => Mode = mode;

        public override void MIDIEnter(Signal n) {
            int x = n.Index % 10;
            int y = n.Index / 10;

            if (Mode == FlipType.Horizontal) x = 9 - x;
            else if (Mode == FlipType.Vertical) y = 9 - y;

            else if (Mode == FlipType.Diagonal1) {
                int temp = x;
                x = y;
                y = temp;
            
            } else if (Mode == FlipType.Diagonal2) {
                x = 9 - x;
                y = 9 - y;

                int temp = x;
                x = y;
                y = temp;
            }

            int result = y * 10 + x;
            
            n.Index = (byte)result;
            MIDIExit?.Invoke(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            if (!Enum.TryParse(data["mode"].ToString(), out FlipType mode)) return null;

            return new Flip(mode);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("mode");
                        writer.WriteValue(Mode);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}