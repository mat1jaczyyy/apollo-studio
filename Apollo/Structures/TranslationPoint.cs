using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Apollo.Structures {
    public class TranslationPoint {
        public static readonly string Identifier = "translationpoint";

        private int _x = 0;
        public int X {
            get => _x;
            set {
                if (-9 <= value && value <= 9)
                    _x = value;
            }
        }

        private int _y = 0;
        public int Y {
            get => _y;
            set {
                if (-9 <= value && value <= 9)
                    _y = value;
            }
        }

        public TranslationPoint(int x = 0, int y = 0) {
            X = x;
            Y = y;
        }

        public static TranslationPoint Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new TranslationPoint(
                int.Parse(data["x"].ToString()),
                int.Parse(data["y"].ToString())
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

                        writer.WritePropertyName("x");
                        writer.WriteValue(X);

                        writer.WritePropertyName("y");
                        writer.WriteValue(Y);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
        
            return json.ToString();
        }
    }
}