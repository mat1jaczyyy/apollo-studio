using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Avalonia.Controls;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Viewers;

namespace Apollo.Elements {
    public class Project {
        public static readonly string Identifier = "project";

        public Window Window;

        public List<Track> Tracks;
        public Decimal BPM;

        private string _path;
        public string FilePath {
            get => _path;
        }

        public async void Save(Window sender) {
            if (_path == "") {
                SaveFileDialog sfd = new SaveFileDialog() {
                    Filters = new List<FileDialogFilter>() {
                        new FileDialogFilter() {
                            Extensions = new List<string>() {
                                "approj"
                            },
                            Name = "Apollo Project"
                        }
                    },
                    Title = "Save Project"
                };

                string result = await sfd.ShowAsync(sender);
                if (result != null) {
                    _path = result;
                }
            }

            string[] file = _path.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                File.WriteAllText(_path, Encode());
            }
        }

        public Project(Decimal bpm = 150, List<Track> tracks = null, string path = "") {
            if (tracks == null)
                tracks = (from i in Core.MIDI.Devices where i.Available select new Track() { Launchpad = i }).ToList();

            for (int i = 0; i < tracks.Count; i++)
                tracks[i].ParentIndex = i;

            BPM = bpm;
            Tracks = tracks;
            _path = path;
        }

        public void Dispose() {
            foreach (Track track in Tracks)
                track.Dispose();
        }

        public static Project Decode(string jsonString, string path) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            List<object> tracks = JsonConvert.DeserializeObject<List<object>>(data["tracks"].ToString());

            return new Project(Decimal.Parse(data["bpm"].ToString()), (from i in tracks select Track.Decode(i.ToString())).ToList(), path);
        }

        public string Encode() {
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