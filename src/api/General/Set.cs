using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;

using api.Devices;

namespace api {
    public static class Set {
        public static List<Track> Tracks = new List<Track>();
        public static Decimal BPM = 120;

        public static void Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "set") return;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            Dictionary<string, object> tracks = JsonConvert.DeserializeObject<Dictionary<string, object>>(data["tracks"].ToString());

            Close();

            BPM = Decimal.Parse(data["bpm"].ToString());
            for (int i = 0; i < int.Parse(tracks["count"].ToString()); i++) {
                Tracks.Add(Track.Decode(tracks[i.ToString()].ToString()));
            }
        }

        private static void Close() {
            foreach (Track track in Tracks)
                track.Dispose();

            Tracks = new List<Track>();
        }

        public static void New() {
            Close();
            Tracks.Add(new Track());
        }

        public static void Open(string path) {
            if (File.Exists(path)) {
                Decode(File.ReadAllText(path));
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

                        writer.WritePropertyName("bpm");
                        writer.WriteValue(BPM);

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