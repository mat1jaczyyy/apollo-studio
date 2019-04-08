using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Structures {
    public class Signal {
        public static readonly string Identifier = "signal";

        private byte _p;
        public Color Color;
        public int Layer;

        public byte Index {
            get => _p;
            set {
                if (1 <= value && value <= 99)
                    _p = value;
            }
        }

        public Signal Clone() => new Signal(_p, Color.Clone(), Layer);

        public Signal(byte index = 11, Color color = null, int layer = 0, Launchpad.InputType format = Launchpad.InputType.XY) {
            if (format == Launchpad.InputType.DrumRack)
                index = Conversion.DRtoXY[index];

            if (color == null) color = new Color(63);
            _p = 11;

            Index = index;
            Color = color;
            Layer = layer;
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();
                    
                        writer.WritePropertyName("index");
                        writer.WriteValue(_p);
                        
                        writer.WritePropertyName("color");
                        writer.WriteRawValue(Color.Encode());

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override string ToString() => $"{Index} @ {Layer} / {Color.Red}, {Color.Green}, {Color.Blue}";
    }
}