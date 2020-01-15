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
using Apollo.Interfaces;
using Apollo.Undo;
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

        public bool IsDisposing { get; private set; } = false;

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

        public event ChangedEventHandler TrackOperationFinished;
        bool _trackoperation = false;
        public bool TrackOperation {
            get => _trackoperation;
            set {
                if (_trackoperation != value && (_trackoperation = value) == false) {
                    TrackOperationFinished?.Invoke();
                    TrackOperationFinished = null;
                }
            }
        }

        public event ChangedEventHandler PathChanged;
        string _path;
        public string FilePath {
            get => _path;
            set {
                _path = Preferences.CrashPath = value;
                PathChanged?.Invoke();
            }
        }

        public string FileName => Path.GetFileNameWithoutExtension(FilePath);

        public delegate void MacroChangedEventHandler();
        public event MacroChangedEventHandler MacroChanged;

        public int[] Macros;
        
        public void SetMacro(int target, int value) {
            if (1 <= value && value <= 100) {
                Macros[target - 1] = value;
                MacroChanged?.Invoke();
            }
        }
        
        public int GetMacro(int target) => Macros[target - 1];

        public async void WriteCrashBackup() {
            if (!Directory.Exists(Program.CrashDir)) Directory.CreateDirectory(Program.CrashDir);
            
            await WriteFile(null, Program.CrashProject, false);
        }

        public async Task<bool> WriteFile(Window sender, string path = null, bool store = true) {
            if (path == null) path = FilePath;

            try {
                if (!Directory.Exists(Path.GetDirectoryName(path))) throw new UnauthorizedAccessException();
                File.WriteAllBytes(path, Encoder.Encode(this).ToArray());

            } catch (UnauthorizedAccessException) {
                if (sender != null) await MessageWindow.Create(
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
            TrackCountChanged?.Invoke(Tracks.Count);

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
            TrackOperation = true;
            Tracks.Insert(index, track);
            Reroute();
            TrackOperation = false;

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

            TrackOperation = true;
            if (dispose) Tracks[index].Dispose();
            Tracks.RemoveAt(index);
            Reroute();
            TrackOperation = false;
        }

        public Project(int bpm = 150, int[] macros = null, List<Track> tracks = null, string author = "", long basetime = 0, long started = 0, string path = "") {
            TimeSpent.Start();

            BPM = bpm;
            Macros = macros?? new int[4] {1, 1, 1, 1};
            Tracks = tracks?? (from i in MIDI.Devices where i.Available && i.Type != LaunchpadType.Unknown select new Track() { Launchpad = i }).ToList();
            Author = author;
            BaseTime = basetime;
            FilePath = path;
            Started = (started == 0)? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds(started);

            if (Tracks.Count == 0 && tracks == null) Tracks.Insert(0, new Track());

            Reroute();
        }
        
        public async Task<bool> HandleKey(Window sender, KeyEventArgs e) {
            if (e.KeyModifiers == App.ControlKey) {
                if (e.Key == Key.S) await Save(sender);
                else if (e.Key == Key.F) MIDI.ClearState();
                else return false;

            } else if (e.KeyModifiers == (App.ControlKey | KeyModifiers.Shift)) {
                if (e.Key == Key.S) await Save(sender, true);
                else if (e.Key == Key.W) {
                    if (Window == null) ProjectWindow.Create(sender);
                    Window.CloseForce(true);
                
                } else return false;
            
            } else return false;

            return true;
        }

        public void Dispose() {
            IsDisposing = true;

            Undo?.Dispose();
            Undo = null;

            MacroChanged = null;
            PathChanged = null;
            TrackCountChanged = null;

            Window = null;

            foreach (Track track in Tracks) track.Dispose();
            foreach (Launchpad launchpad in MIDI.Devices) launchpad.Clear();

            TimeSpent.Stop();
        }
    }
}