using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Apollo.Structures {
    public class Frame {
        public static readonly string Identifier = "frame";

        public Color[] Screen;

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;

        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public string TimeString => Mode? Length.ToString() : $"{Time}ms";

        public Frame Clone() => new Frame(Mode, Length.Clone(), Time, (from i in Screen select i.Clone()).ToArray());

        public Frame(bool mode = false, Length length = null, int time = 1000, Color[] screen = null) {
            if (screen == null || screen.Length != 100) {
                screen = new Color[100];
                for (int i = 0; i < 100; i++) screen[i] = new Color(0);
            }

            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Screen = screen;
        }

        public static Frame Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> screen = JsonConvert.DeserializeObject<List<object>>(data["screen"].ToString());
            Color[] colors = new Color[100];

            for (int i = 0; i < 100; i++)
                colors[i] = Color.Decode(screen[i].ToString());

            return new Frame(
                Convert.ToBoolean(data["mode"]),
                Length.Decode(data["length"].ToString()),
                Convert.ToInt32(data["time"]),
                colors
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

                        writer.WritePropertyName("mode");
                        writer.WriteValue(Mode);

                        writer.WritePropertyName("length");
                        writer.WriteRawValue(Length.Encode());

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("screen");
                        writer.WriteStartArray();

                            for (int i = 0; i < 100; i++)
                                writer.WriteRawValue(Screen[i].Encode());

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
        
            return json.ToString();
        }
    }
}