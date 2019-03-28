using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Avalonia.Controls;

using Newtonsoft.Json;

using Apollo.Windows;

namespace Apollo.Elements {
    public class Project {
        public static readonly string Identifier = "project";

        public ProjectWindow Window;

        public List<Track> Tracks;

        public Decimal BPM;

        public delegate void PathChangedEventHandler(string path);
        public event PathChangedEventHandler PathChanged;

        private string _path;
        public string FilePath {
            get => _path;
            private set {
                _path = value;
                PathChanged?.Invoke(_path);
            }
        }

        public async void Save(Window sender) {
            if (FilePath == "") {
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
                    FilePath = result;
                }
            }

            string[] file = FilePath.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                File.WriteAllText(FilePath, Encode());
            }
        }

        private void Reroute() {
            for (int i = 0; i < Tracks.Count; i++)
                Tracks[i].ParentIndex = i;
        }

        public Track this[int index] {
            get => Tracks[index];
        }

        public int Count {
            get => Tracks.Count;
        }

        public void Insert(int index, Track track) {
            Tracks.Insert(index, track);
            Reroute();
        }

        public void Add(Track track) {
            Tracks.Add(track);
            Reroute();
        }

        public void Remove(int index) {
            Tracks.RemoveAt(index);
            Reroute();
        }

        public Project(Decimal bpm = 150, List<Track> tracks = null, string path = "") {
            if (tracks == null)
                tracks = (from i in Core.MIDI.Devices where i.Available select new Track() { Launchpad = i }).ToList();

            BPM = bpm;
            Tracks = tracks;
            FilePath = path;

            Reroute();
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