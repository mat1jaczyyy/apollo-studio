using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

using Avalonia.Controls;
using Avalonia.Input;

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

        public string FileName {
            get => Path.GetFileNameWithoutExtension(FilePath);
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

        private void WriteFile(Window sender, string path = null, bool store = true) {
            if (path == null) path = FilePath;

            string[] file = path.Split(Path.DirectorySeparatorChar);

            if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                try {
                    File.WriteAllBytes(path, Encoder.Encode(this).ToArray());

                } catch (UnauthorizedAccessException e) {
                    ErrorWindow.Create(
                        $"An error occurred while writing the file to disk:\n\n{e.Message}\n\n" +
                        "You may not have sufficient privileges to write to the destination folder, or the current file already exists but cannot be overwritten.",
                        sender
                    );

                    return;
                }

                Undo.SavePosition();
                if (store) FilePath = path;
            }
        }

        public void Save(Window sender) {
            if (FilePath == "") Save(sender, true);
            else WriteFile(sender);
        }

        public async void Save(Window sender, bool store) {
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

            if (result != null) WriteFile(sender, result, store);
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

            if (Tracks.Count == 0 && tracks == null) Tracks.Insert(0, new Track());

            Reroute();
        }
        
        public bool HandleKey(Window sender, KeyEventArgs e) {
            if (e.Modifiers == InputModifiers.Control) {
                if (e.Key == Key.S) Save(sender);
                else return false;

            } else if (e.Modifiers == (InputModifiers.Control | InputModifiers.Shift)) {
                if (e.Key == Key.S) Save(sender, true);
                else return false;
            
            } else return false;

            return true;
        }

        public void Dispose() {
            Undo.Window?.Close();

            foreach (Track track in Tracks)
                track.Dispose();
            
            foreach (Launchpad launchpad in MIDI.Devices)
                launchpad.Clear();
        }
    }
}