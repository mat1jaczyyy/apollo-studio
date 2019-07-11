using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;

using Apollo.Binary;
using Apollo.Core;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Interfaces;
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

        public delegate void ChangedEventHandler();
        public event ChangedEventHandler BPMChanged;

        int _bpm;
        public int BPM {
            get => _bpm;
            set {
                if (20 <= value && value <= 999) {
                    _bpm = value;

                    BPMChanged?.Invoke();

                    Window?.SetBPM(_bpm.ToString());
                }
            }
        }

        string _author;
        public string Author {
            get => _author;
            set {
                _author = value;
                Window?.SetAuthor(_author);
            }
        }

        public long BaseTime = 0;
        public long Time => BaseTime + (long)TimeSpent.Elapsed.TotalSeconds;

        public DateTimeOffset Started { get; private set; }

        Stopwatch TimeSpent = new Stopwatch();

        public UndoManager Undo = new UndoManager();

        public event ChangedEventHandler PathChanged;

        string _path;
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

        int _page;
        public int Page {
            get => _page;
            set {
                if (1 <= value && value <= 100) {
                    _page = value;
                    PageChanged?.Invoke();
                }
            }
        }

        public async Task<bool> WriteFile(Window sender, string path = null, bool store = true, bool error = true) {
            if (path == null) path = FilePath;


            try {
                if (!Directory.Exists(Path.GetDirectoryName(path))) throw new UnauthorizedAccessException();
                File.WriteAllBytes(path, Encoder.Encode(this).ToArray());

            } catch (UnauthorizedAccessException) {
                if (error) await MessageWindow.Create(
                    $"An error occurred while writing the file.\n\n" +
                    "You may not have sufficient privileges to write to the destination folder, or\n" +
                    "the current file already exists but cannot be overwritten.",
                    null, sender
                );

                return false;
            }

            if (store) {
                Undo.SavePosition();
                FilePath = path;

                if (Preferences.Backup) {
                    string dir = Path.Combine(Path.GetDirectoryName(FilePath), $"{FileName} Backups");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    File.Copy(FilePath, Path.Join(dir, $"{FileName} Backup {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.approj"));
                }
            }

            return true;
        }

        public async Task<bool> Save(Window sender) {
            if (Undo.Saved) return false;

            bool ret = (FilePath == "")
                ? await Save(sender, true)
                : await WriteFile(sender);

            if (ret) Preferences.RecentsAdd(FilePath);

            return ret;
        }

        public async Task<bool> Save(Window sender, bool store) {
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
            
            bool ret = (result != null)
                ? await WriteFile(sender, result, store)
                : false;
            
            if (ret) Preferences.RecentsAdd(result);
            
            return ret;
        }

        public delegate void TrackCountChangedEventHandler(int value);
        public event TrackCountChangedEventHandler TrackCountChanged;

        void Reroute() {
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
            if (index < Tracks.Count - 1)
                Window?.Selection.Select(Tracks[index + 1]);
            else if (Tracks.Count > 1)
                Window?.Selection.Select(Tracks[Tracks.Count - 2]);
            else
                Window?.Selection.Select(null);

            Window?.Contents_Remove(index);
            Tracks[index].Window?.Close();

            if (dispose) Tracks[index].Dispose();
            Tracks.RemoveAt(index);
            Reroute();
        }

        public Project(int bpm = 150, int page = 1, List<Track> tracks = null, string author = "", long basetime = 0, long started = 0, string path = "") {
            TimeSpent.Start();

            BPM = bpm;
            Page = page;
            Tracks = tracks?? (from i in MIDI.Devices where i.Available && i.Type != LaunchpadType.Unknown select new Track() { Launchpad = i }).ToList();
            Author = author;
            BaseTime = basetime;
            FilePath = path;
            Started = (started == 0)? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds(started);

            if (Tracks.Count == 0 && tracks == null) Tracks.Insert(0, new Track());

            Reroute();
        }
        
        public async Task<bool> HandleKey(Window sender, KeyEventArgs e) {
            if (e.Modifiers == Program.ControlKey) {
                if (e.Key == Key.S) await Save(sender);
                else return false;

            } else if (e.Modifiers == (Program.ControlKey | InputModifiers.Shift)) {
                if (e.Key == Key.S) await Save(sender, true);
                else return false;
            
            } else return false;

            return true;
        }

        public void Dispose() {
            Undo?.Dispose();
            Undo = null;

            PageChanged = null;
            PathChanged = null;
            TrackCountChanged = null;

            Window = null;

            foreach (Track track in Tracks) track.Dispose();
            foreach (Launchpad launchpad in MIDI.Devices) launchpad.Clear();

            TimeSpent.Stop();
        }
    }
}