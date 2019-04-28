using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public (double, double, double) ToHSV() {
            double r = Red / 63.0;
            double g = Green / 63.0;
            double b = Blue / 63.0;
            double[] colors = new double[] {r, g, b};

            double min = colors.Min();
            double max = colors.Max();

            double hue = 0;
            if (min != max) {
                double diff = max - min;

                if (max == r) {
                    hue = (g - b) / diff;
                } else if (max == g) {
                    hue = (b - r) / diff + 2.0;
                } else if (max == b) {
                    hue = (r - g) / diff + 4.0;
                }
                if (hue < 0) hue += 6.0;
            }

            double saturation = 0;
            if (max != 0) saturation = 1 - (min / max);

            return (hue * 60, saturation, max);
        }

        public Color FromHSV(double hue, double saturation, double value) {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue - Math.Floor(hue);
            value *= 63;

            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)      return new Color(v, t, p);
            else if (hi == 1) return new Color(q, v, p);
            else if (hi == 2) return new Color(p, v, t);
            else if (hi == 3) return new Color(p, q, v);
            else if (hi == 4) return new Color(t, p, v);
            else              return new Color(v, p, q);
        }

        public override bool Equals(object obj) {
            if (!(obj is Color)) return false;
            return this == (Color)obj;
        }

        public static bool operator ==(Color a, Color b) => a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue;
        public static bool operator !=(Color a, Color b) => !(a == b);

        public override int GetHashCode() => HashCode.Combine(Red, Green, Blue);

        public AvaloniaColor ToAvaloniaColor() => new AvaloniaColor(
            255,
            (byte)(_r * (255.0 / 63)),
            (byte)(_g * (255.0 / 63)),
            (byte)(_b * (255.0 / 63))
        );

        public IBrush ToBrush() => new SolidColorBrush(ToAvaloniaColor());

        public string ToHex() => $"#{Red.ToString("X2")}{Green.ToString("X2")}{Blue.ToString("X2")}";

        public override string ToString() => $"({Red}, {Green}, {Blue})";

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