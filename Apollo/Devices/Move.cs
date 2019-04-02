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

        private int _x;
        public int X {
            get => _x;
            set {
                if (-9 <= value && value <= 9)
                    _x = value;
            }
        }

        private int _y;
        public int Y {
            get => _y;
            set {
                if (-9 <= value && value <= 9)
                    _y = value;
            }
        }

        public override Device Clone() => new Move(_x, _y);

        public Move(int x = 0, int y = 0): base(DeviceIdentifier) {
            X = x;
            Y = y;
        }

        public override void MIDIEnter(Signal n) {
            int x = n.Index % 10 + X;
            int y = n.Index / 10 + Y;
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
                Convert.ToInt32(data["x"]),
                Convert.ToInt32(data["y"])
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

                        writer.WritePropertyName("x");
                        writer.WriteValue(_x);

                        writer.WritePropertyName("y");
                        writer.WriteValue(_y);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}