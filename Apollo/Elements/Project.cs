using System.Collections.Generic;
using System.Linq;
using System.IO;

using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Project: ISelectParent {
        public ISelectParentViewer IViewer {
            get => Window;
        }

        public List<ISelect> IChildren {
            get => Tracks.Select(i => (ISelect)i).ToList();
        }

        public ProjectWindow Window;

        public List<Track> Tracks;

        public int BPM;

        public delegate void PathChangedEventHandler();
        public event PathChangedEventHandler PathChanged;

        private string _path;
        public string FilePath {
            get => _path;
            set {
                _path = value;
                PathChanged?.Invoke();
            }
        }

        public delegate void PageChangedEventHandler();
        public event PageChangedEventHandler PageChanged;

        private int _page;
        public int Page {
            get => _page;
            set {
                if (1 <= value && value <= 100) {
                    _page = value;
                    PageChanged?.Invoke();
                }
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
                if (result != null) FilePath = result;
            }

            string[] file = FilePath.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1))))
                File.WriteAllBytes(FilePath, Encoder.Encode(this).ToArray());
        }

        public delegate void TrackCountChangedEventHandler(int value);
        public event TrackCountChangedEventHandler TrackCountChanged;

        private void Reroute() {
            for (int i = 0; i < Tracks.Count; i++)
                Tracks[i].ParentIndex = i;
            
            TrackCountChanged?.Invoke(Tracks.Count);
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

        public void Remove(int index, bool dispose = true) {
            if (dispose) Tracks[index].Dispose();
            Tracks.RemoveAt(index);
            Reroute();
        }

        public Project(int bpm = 150, int page = 1, List<Track> tracks = null, string path = "") {
            BPM = bpm;
            Page = page;
            Tracks = tracks?? (from i in MIDI.Devices where i.Available && i.Type != Launchpad.LaunchpadType.Unknown select new Track() { Launchpad = i }).ToList();
            FilePath = path;

            Reroute();
        }

        public void Dispose() {
            foreach (Track track in Tracks)
                track.Dispose();
            
            foreach (Launchpad launchpad in MIDI.Devices)
                launchpad.Clear();
        }
    }
}