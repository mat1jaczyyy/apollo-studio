using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Core;
using Apollo.Helpers;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Project: ISelectParent {
        public ISelectParentViewer IViewer {
            get => Window;
        }

        public List<ISelect> IChildren {
            get => Tracks.Select(i => (ISelect)i).ToList();
        }

        public bool IRoot {
            get => true;
        }

        public ProjectWindow Window;

        public List<Track> Tracks;

        private int _bpm;
        public int BPM {
            get => _bpm;
            set {
                if (20 <= value && value <= 999) {
                    _bpm = value;
                    if (Window != null) Window.SetBPM(_bpm.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public UndoManager Undo = new UndoManager();

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

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                File.WriteAllBytes(FilePath, Encoder.Encode(this).ToArray());
                Undo.SavePosition();
            }
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

            Window?.Contents_Insert(index, Tracks[index]);

            Window?.Selection.Select(Program.Project[index]);
        }

        public void Remove(int index, bool dispose = true) {
            Window?.Contents_Remove(index);
            Tracks[index].Window?.Close();

            if (dispose) Tracks[index].Dispose();
            Tracks.RemoveAt(index);
            Reroute();
            
            if (index < Tracks.Count)
                Window?.Selection.Select(Tracks[index]);
            else if (Tracks.Count > 0)
                Window?.Selection.Select(Tracks.Last());
            else
                Window?.Selection.Select(null);
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