using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;

namespace Apollo.Elements {
    public static class Project {
        public static readonly string Identifier = "project";

        public static List<Track> Tracks = new List<Track>();
        public static Decimal BPM = 150;

        private static string _path = "";
        public static string FilePath {
            get {
                return _path;
            }
        }

        private static void Close() {
            foreach (Track track in Tracks)
                track.Dispose();

            Tracks = new List<Track>();
        }

        public static void New() {
            Close();
            Tracks.Add(new Track() {ParentIndex = Tracks.Count});
            
            Tracks[0].Launchpad = (MIDI.Devices.Count > 0)? MIDI.Devices[0] : new Launchpad("null placeholder");

            _path = "";
        }

        public static bool Open(string path) {
            bool result = File.Exists(path) && Decode(File.ReadAllText(path));
            if (result) _path = path;

            return result;
        }

        public static void Save(string path) {
            string[] file = path.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                File.WriteAllText(path, Encode());
            }

            _path = path;
        }

        public static bool Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return false;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            List<object> tracks = JsonConvert.DeserializeObject<List<object>>(data["tracks"].ToString());

            Close();

            BPM = Decimal.Parse(data["bpm"].ToString());
            foreach (object track in tracks) {
                Track _track = Track.Decode(track.ToString());
                _track.ParentIndex = Tracks.Count;
                Tracks.Add(_track);
            }

            return true;
        }

        public static string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("version");
                        writer.WriteValue("alpha");

                        writer.WritePropertyName("bpm");
                        writer.WriteValue(BPM);
                        
                        writer.WritePropertyName("path");
                        writer.WriteValue(FilePath);

                        writer.WritePropertyName("tracks");
                        writer.WriteStartArray();

                            for (int i = 0; i < Tracks.Count; i++) {
                                writer.WriteRawValue(Tracks[i].Encode());
                            }
                        
                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}