using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Viewers;

namespace Apollo.Elements {
    public class Project: Window {
        public static readonly string Identifier = "project";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        public List<Track> Tracks;
        public Decimal BPM;

        private string _path;
        public string FilePath {
            get => _path;
        }

        public Project(Decimal bpm = 150, List<Track> tracks = null, string path = "") {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));

            if (tracks == null) {
                tracks = new List<Track>() {new Track()};
                tracks[0].Launchpad = (MIDI.Devices.Count > 0)? MIDI.Devices[0] : new Launchpad("null placeholder");
            }

            for (int i = 0; i < tracks.Count; i++)
                tracks[i].ParentIndex = i;

            BPM = bpm;
            Tracks = tracks;
            _path = path;
            
            Controls Contents = this.Get<StackPanel>("Contents").Children;
            
            foreach (Track track in Tracks)
                Contents.Add(new TrackViewer(track));
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

            //return new Project;
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