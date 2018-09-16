using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace api {
    public static class Set {
        public static List<Track> Tracks = new List<Track>();

        public static void New() {
            Tracks = new List<Track>();
            Tracks.Add(new Track());
        }

        public static void Open(string path) {
            if (File.Exists(path)) {
                JsonTextReader reader = new JsonTextReader(new StringReader(File.ReadAllText(path)));
 
                while (reader.Read()) {
                    if (reader.Value != null)
                        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                    else
                        Console.WriteLine("Token: {0}", reader.TokenType);
                }
            }
        }

        public static void Save(string path) {
            string[] file = path.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                if (File.Exists(path)) {
                    // TODO: Ask for overwrite
                }

                File.WriteAllText(path, Encode());
            }
        }

        public static string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("set");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("version");
                        writer.WriteValue("alpha");

                        writer.WritePropertyName("tracks");
                        writer.WriteStartObject();

                            writer.WritePropertyName("count");
                            writer.WriteValue(Tracks.Count);

                            for (int i = 0; i < Tracks.Count; i++) {
                                writer.WritePropertyName(i.ToString());
                                writer.WriteRawValue(Tracks[i].Encode());
                            }
                        
                        writer.WriteEndObject();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}