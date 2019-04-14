using System.Collections.Generic;
using System.IO;
using System.Text;

using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;

using Newtonsoft.Json;

namespace Apollo.Structures {
    public class Color {
        public static readonly string Identifier = "color";

        private byte _r, _g, _b;

        private bool IsValid(byte value) => 0 <= value && value <= 63;

        public byte Red {
            get => _r;
            set {
                if (IsValid(value)) _r = value;
            }
        }

        public byte Green {
            get => _g;
            set {
                if (IsValid(value)) _g = value;
            }
        }

        public byte Blue {
            get => _b;
            set {
                if (IsValid(value)) _b = value;
            }
        }

        public bool Lit {
            get => _r != 0 || _g != 0 || _b != 0;
        }

        public Color Clone() => new Color(_r, _g, _b);

        public Color(byte bright = 63) {
            if (!IsValid(bright)) bright = 63;
            _r = _g = _b = bright;
        }

        public Color(byte red, byte green, byte blue) {
            _r = _g = _b = 63;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public AvaloniaColor ToAvaloniaColor() => new AvaloniaColor(
            255,
            (byte)(_r * (255.0 / 63)),
            (byte)(_g * (255.0 / 63)),
            (byte)(_b * (255.0 / 63))
        );

        public IBrush ToBrush() => new SolidColorBrush(ToAvaloniaColor());

        public string ToHex() => $"#{_r.ToString("X2")}{_g.ToString("X2")}{_b.ToString("X2")}";

        public static Color Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Color(
                byte.Parse(data["red"].ToString()),
                byte.Parse(data["green"].ToString()),
                byte.Parse(data["blue"].ToString())
            );
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("red");
                        writer.WriteValue(_r);

                        writer.WritePropertyName("green");
                        writer.WriteValue(_g);

                        writer.WritePropertyName("blue");
                        writer.WriteValue(_b);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}