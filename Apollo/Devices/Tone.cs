using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Tone: Device {
        public static readonly new string DeviceIdentifier = "tone";

        private decimal _h, _s, _v;

        public decimal Hue {
            get => _h;
            set {
                if (-180 <= value && value <= 180)
                    _h = value;
            }
        }

        public decimal Saturation {
            get => _s;
            set {
                if (0 <= value && value <= 4)
                    _s = value;
            }
        }

        public decimal Value {
            get => _v;
            set {
                if (0 <= value && value <= 4)
                    _v = value;
            }
        }

        public override Device Clone() => new Tone(Hue, Saturation, Value);

        public Tone(decimal hue = 0, decimal saturation = 1, decimal value = 1): base(DeviceIdentifier) {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit) {
                (double hue, double saturation, double value) = n.Color.ToHSV();

                hue = (hue + (double)Hue) % 360;
                saturation = Math.Min(1, saturation * (double)Saturation);
                value = Math.Min(1, value * (double)value);

                n.Color = Color.FromHSV(hue, saturation, value);
            }

            MIDIExit?.Invoke(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Tone(
                Convert.ToDecimal(data["hue"].ToString()),
                Convert.ToDecimal(data["saturation"].ToString()),
                Convert.ToDecimal(data["value"].ToString())
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

                        writer.WritePropertyName("hue");
                        writer.WriteValue(Hue);

                        writer.WritePropertyName("saturation");
                        writer.WriteValue(Saturation);

                        writer.WritePropertyName("value");
                        writer.WriteValue(Value);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}