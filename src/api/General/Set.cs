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
        public static readonly string Identifier = "set";

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
            
            Set.Tracks[0].Launchpad = (MIDI.Devices.Count > 0)? MIDI.Devices[0] : new Launchpad("null placeholder");

            _path = "";
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

        public static string Request(Dictionary<string, object> data, List<string> path = null) {
            if (path == null) path = new List<string>();
            path.Insert(0, Identifier);

            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("message");

                    writer.WritePropertyName("path");
                    writer.WriteValue(string.Join('/', path));

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        foreach (KeyValuePair<string, object> entry in data) {
                            writer.WritePropertyName(entry.Key);
                            string value = entry.Value.ToString();

                            if (value[0].Equals('{') || value[0].Equals('['))
                                writer.WriteRawValue(value);
                            else
                                writer.WriteValue(value);
                        }

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }

            return json.ToString();
        }

        public static ObjectResult Respond(string obj, string[] path, Dictionary<string, object> data) {
            if (path[0] != Identifier) return new BadRequestObjectResult("Incorrect recipient for message.");

            if (path.Count() > 1) {
                if (path[1].StartsWith("track:"))
                    return Tracks[Convert.ToInt32(path[1].Split(':')[1])].Respond(obj, path.Skip(1).ToArray(), data);

                else if (path[1] == "midi")
                    return MIDI.Respond(obj, path.Skip(1).ToArray(), data);
                
                else return new BadRequestObjectResult("Incorrectly formatted message.");
            }

            switch (data["type"].ToString()) {
                case "new":
                    New();
                    return new OkObjectResult(Set.Encode());

                case "open":
                    if (Open(data["path"].ToString())) {
                        _path = data["path"].ToString();
                        return new OkObjectResult(Set.Encode());
                    }
                    return new BadRequestObjectResult("Bad Set file content or path to Set file.");

                case "save":
                    if (_path == "")
                        return new BadRequestObjectResult("No path to save to");

                    Save(_path);
                    return new OkObjectResult(null);
                
                case "save_as":
                    Save(data["path"].ToString());
                    return new OkObjectResult(Set.Encode());

                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}