using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Structures {
    public class Signal {
        public static readonly string Identifier = "signal";

        public Launchpad Source;
        private byte _p = 11;
        public Color Color;
        public int Layer;
        public int? MultiTarget;

        public byte Index {
            get => _p;
            set {
                if (1 <= value && value <= 99)
                    _p = value;
            }
        }

        public Signal Clone() => new Signal(Source, Index, Color.Clone(), Layer, MultiTarget);

        public Signal(Launchpad source, byte index = 11, Color color = null, int layer = 0, int? multiTarget = null) {
            Source = source;
            Index = index;
            Color = color?? new Color(63);
            Layer = layer;
            MultiTarget = multiTarget;
        }

        public Signal(Launchpad.InputType input, Launchpad source, byte index = 11, Color color = null, int layer = 0, int? multiTarget = null): this(
            source,
            (input == Launchpad.InputType.DrumRack)? Conversion.DRtoXY[index] : index,
            color,
            layer,
            multiTarget
        ) {}

        public override bool Equals(object obj) {
            if (!(obj is Signal)) return false;
            return this == (Signal)obj;
        }

        public static bool operator ==(Signal a, Signal b) => a.Source.Equals(b.Source) && a.Index == b.Index && a.Color == b.Color && a.Layer == b.Layer && a.MultiTarget == b.MultiTarget;
        public static bool operator !=(Signal a, Signal b) => !(a == b);
        
        public override int GetHashCode() => ToString().GetHashCode();

        public override string ToString() => $"{Source.Name} -> {Index} @ {Layer} & {MultiTarget} = {Color}";

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
    }
}