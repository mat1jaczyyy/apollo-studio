using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;

namespace Apollo.Structures {
    public class Length {
        public static readonly string Identifier = "length";

        private static string[] Steps = new string[]
            {"1/128", "1/64", "1/32", "1/16", "1/8", "1/4", "1/2", "1/1", "2/1", "4/1"};

        private int _value;
        public int Step {
            get => _value;
            set {
                if (0 <= value && value <= 9)
                    _value = value;
            }
        }
        
        public Decimal Value {
            get => Convert.ToDecimal(Math.Pow(2, _value - 7));
        }

        public Length(int step = 5) => Step = step;

        public static implicit operator Decimal(Length x) => x.Value * 240000 / Program.Project.BPM;

        public override string ToString() => Steps[_value];

        public static Length Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Length(
                int.Parse(data["value"].ToString())
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

                        writer.WritePropertyName("value");
                        writer.WriteValue(_value.ToString());

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
        
            return json.ToString();
        }
    }
}