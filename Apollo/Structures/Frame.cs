using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Apollo.Structures {
    public class Frame {
        public static readonly string Identifier = "frame";

        public Color[] Screen;

        private int _time = 200;
        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public Frame(Color[] screen = null, int time = 200) {
            if (screen == null || screen.Length != 100) {
                screen = new Color[100];
                for (int i = 0; i < 100; i++) screen[i] = new Color(0);
            }
            Screen = screen;
            Time = time;
        }

        public static Frame Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> screen = JsonConvert.DeserializeObject<List<object>>(data["screen"].ToString());
            Color[] colors = new Color[100];

            for (int i = 0; i < 100; i++)
                colors[i] = Color.Decode(screen[i - 1].ToString());

            return new Frame(
                colors,
                int.Parse(data["time"].ToString())
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

                        writer.WritePropertyName("screen");
                        writer.WriteStartArray();

                            for (int i = 0; i < 100; i++)
                                writer.WriteRawValue(Screen[i].Encode());

                        writer.WriteEndArray();

                        writer.WritePropertyName("time");
                        writer.WriteValue(Time);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
        
            return json.ToString();
        }
    }
}