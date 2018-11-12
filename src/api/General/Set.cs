using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api.Devices;

namespace api {
    public static class Set {
        public static List<Track> Tracks = new List<Track>();
        public static Decimal BPM = 150;

        private static void Close() {
            foreach (Track track in Tracks)
                track.Dispose();

            Tracks = new List<Track>();
        }

        public static void New() {
            Close();
            Tracks.Add(new Track());
            Set.Tracks[0].Launchpad = MIDI.Devices[0];
        }

        public static bool Open(string path) {
            if (File.Exists(path)) {
                return Decode(File.ReadAllText(path));
            }
            return false;
        }

        public static void Save(string path) {
            string[] file = path.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                File.WriteAllText(path, Encode());
            }
        }

        public static bool Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "set") return false;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            Dictionary<string, object> tracks = JsonConvert.DeserializeObject<Dictionary<string, object>>(data["tracks"].ToString());

            Close();

            BPM = Decimal.Parse(data["bpm"].ToString());
            for (int i = 0; i < Convert.ToInt32(tracks["count"]); i++) {
                Tracks.Add(Track.Decode(tracks[i.ToString()].ToString()));
            }

            return true;
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

        public static ObjectResult Request(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != "set") return new BadRequestObjectResult("Incorrect recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    switch (data["forward"].ToString()) {
                        case "track":
                            return Tracks[Convert.ToInt32(data["index"])].Request(data["message"].ToString());
                        
                        default:
                            return new BadRequestObjectResult("Incorrectly formatted message.");
                    }
                
                case "new":
                    New();
                    return new OkObjectResult(Set.Encode());

                case "open":
                    if (Open(data["path"].ToString())) {
                        return new OkObjectResult(Set.Encode());
                    }
                    return new BadRequestObjectResult("Bad Set file content or path to Set file.");

                case "save":
                    Save(data["path"].ToString());
                    return new OkObjectResult(null);

                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}