using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;

namespace Apollo.Structures {
    public class Color {
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

        public static Color FromHSV(double hue, double saturation, double value) {
            hue /= 60;

            int hi = Convert.ToInt32(Math.Floor(hue)) % 6;
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

        public static bool operator ==(Color a, Color b) {
            if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, b);
            return a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue;
        }
        public static bool operator !=(Color a, Color b) => !(a == b);

        public override int GetHashCode() => HashCode.Combine(Red, Green, Blue);

        public AvaloniaColor ToAvaloniaColor() => new AvaloniaColor(
            255,
            (byte)(_r * (255.0 / 63)),
            (byte)(_g * (255.0 / 63)),
            (byte)(_b * (255.0 / 63))
        );

        public SolidColorBrush ToBrush() => new SolidColorBrush(ToAvaloniaColor());
        public SolidColorBrush ToScreenBrush() {
            double max = new byte[] {_r, _g, _b}.Max() / 63.0;

            AvaloniaColor bg = (AvaloniaColor)Application.Current.Styles.FindResource("ThemeForegroundLowColor");

            return new SolidColorBrush(new AvaloniaColor(
                255,
                (byte)(_r * (255 / 63.0) * max + bg.R * (1 - max)),
                (byte)(_g * (255 / 63.0) * max + bg.G * (1 - max)),
                (byte)(_b * (255 / 63.0) * max + bg.B * (1 - max))
            ));
        }

        public string ToHex() => $"#{Red.ToString("X2")}{Green.ToString("X2")}{Blue.ToString("X2")}";

        public override string ToString() => $"({Red}, {Green}, {Blue})";
    }
}