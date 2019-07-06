using System;
using System.Collections.Generic;
using System.IO;

using Apollo.Binary;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Windows;

namespace Apollo.Core {
    public static class Preferences {
        public static PreferencesWindow Window;

        static readonly string DirPath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".apollostudio");
        static readonly string FilePath = Path.Combine(DirPath, "Apollo.config");

        public delegate void CheckBoxChanged(bool newValue);
        public delegate void SmoothnessChanged(double newValue);
        public delegate void Changed();

        public static event CheckBoxChanged AlwaysOnTopChanged;
        static bool _AlwaysOnTop = true;
        public static bool AlwaysOnTop {
            get => _AlwaysOnTop;
            set {
                _AlwaysOnTop = value;
                AlwaysOnTopChanged?.Invoke(_AlwaysOnTop);
                Save();
            }
        }

        public static event CheckBoxChanged CenterTrackContentsChanged;
        static bool _CenterTrackContents = true;
        public static bool CenterTrackContents {
            get => _CenterTrackContents;
            set {
                _CenterTrackContents = value;
                CenterTrackContentsChanged?.Invoke(_CenterTrackContents);
                Save();
            }
        }

        public static event Changed LaunchpadStyleChanged;
        static LaunchpadStyles _LaunchpadStyle = LaunchpadStyles.Stock;
        public static LaunchpadStyles LaunchpadStyle {
            get => _LaunchpadStyle;
            set {
                _LaunchpadStyle = value;
                LaunchpadStyleChanged?.Invoke();
                Save();
            }
        }

        public static event Changed LaunchpadGridRotationChanged;
        static bool _LaunchpadGridRotation = false;
        public static bool LaunchpadGridRotation {
            get => _LaunchpadGridRotation;
            set {
                _LaunchpadGridRotation = value;
                LaunchpadGridRotationChanged?.Invoke();
                Save();
            }
        }

        static bool _AutoCreateKeyFilter = true;
        public static bool AutoCreateKeyFilter {
            get => _AutoCreateKeyFilter;
            set {
                _AutoCreateKeyFilter = value;
                Save();
            }
        }

        static bool _AutoCreatePageFilter = false;
        public static bool AutoCreatePageFilter {
            get => _AutoCreatePageFilter;
            set {
                _AutoCreatePageFilter = value;
                Save();
            }
        }

        static bool _AutoCreatePattern = false;
        public static bool AutoCreatePattern {
            get => _AutoCreatePattern;
            set {
                _AutoCreatePattern = value;
                Save();
            }
        }

        public static event SmoothnessChanged FadeSmoothnessChanged;
        public static double FadeSmoothnessSlider { get; private set; } = 1;
        static double _FadeSmoothness;
        public static double FadeSmoothness {
            get => _FadeSmoothness;
            set {
                if (0 <= value && value <= 1) {
                    FadeSmoothnessSlider = value;
                    _FadeSmoothness = 1000 / (1081.45 * Math.Pow(Math.Log(1 - value), 2) + 2);
                    FadeSmoothnessChanged?.Invoke(_FadeSmoothness);
                    Save();
                }
            }
        }

        static bool _CopyPreviousFrame = false;
        public static bool CopyPreviousFrame {
            get => _CopyPreviousFrame;
            set {
                _CopyPreviousFrame = value;
                Save();
            }
        }

        static bool _CaptureLaunchpad = false;
        public static bool CaptureLaunchpad {
            get => _CaptureLaunchpad;
            set {
                _CaptureLaunchpad = value;
                Save();
            }
        }

        static bool _EnableGestures = false;
        public static bool EnableGestures {
            get => _EnableGestures;
            set {
                _EnableGestures = value;
                Save();
            }
        }

        static string _PaletteName = "mat1jaczyyyPalette";
        public static string PaletteName {
            get => _PaletteName;
            set {
                _PaletteName = value;
                Save();
            }
        }

        static Palette _CustomPalette = Palette.mat1jaczyyyPalette;
        public static Palette CustomPalette {
            get => _CustomPalette;
            set {
                _CustomPalette = value;
                Save();
            }
        }

        static Palettes _ImportPalette = Palettes.NovationPalette;
        public static Palettes ImportPalette {
            get => _ImportPalette;
            set {
                _ImportPalette = value;

                if (_ImportPalette == Palettes.Monochrome) Importer.Palette = Palette.Monochrome;
                else if (_ImportPalette == Palettes.NovationPalette) Importer.Palette = Palette.NovationPalette;
                else if (_ImportPalette == Palettes.CustomPalette) Importer.Palette = CustomPalette;

                Save();
            }
        }

        static Themes _Theme = Themes.Dark;
        public static Themes Theme {
            get => _Theme;
            set {
                _Theme = value;
                Save();
            }
        }

        static bool _Backup = true;
        public static bool Backup {
            get => _Backup;
            set {
                _Backup = value;
                Save();
            }
        }

        static bool _Autosave = true;
        public static bool Autosave {
            get => _Autosave;
            set {
                _Autosave = value;
                Save();
            }
        }

        static bool _UndoLimit = true;
        public static bool UndoLimit {
            get => _UndoLimit;
            set {
                if (_UndoLimit = value)
                    Program.Project?.Undo.Limit();

                Save();
            }
        }

        static bool _DiscordPresence = true;
        public static bool DiscordPresence {
            get => _DiscordPresence;
            set {
                _DiscordPresence = value;
                Discord.Set(DiscordPresence);
                Save();
            }
        }

        static bool _DiscordFilename = false;
        public static bool DiscordFilename {
            get => _DiscordFilename;
            set {
                _DiscordFilename = value;
                Discord.Set(DiscordPresence);
                Save();
            }
        }

        public static event Changed RecentsCleared;
        public static List<string> Recents = new List<string>();

        public static void RecentsAdd(string path) {
            if (Recents.Contains(path)) Recents.Remove(path);

            Recents.Insert(0, path);

            Save();
        }

        public static void RecentsRemove(string path) {
            if (Recents.Contains(path)) Recents.Remove(path);

            Save();
        }

        public static void RecentsClear() {
            Recents.Clear();

            RecentsCleared.Invoke();

            Save();
        }

        static string _CrashName = "";
        public static string CrashName {
            get => _CrashName;
            set {
                _CrashName = value;
                Save();
            }
        }
        static string _CrashPath = "";
        public static string CrashPath {
            get => _CrashPath;
            set {
                _CrashPath = value;
                Save();
            }
        }

        public static void Save() {
            if (!Directory.Exists(DirPath)) Directory.CreateDirectory(DirPath);

            try {
                File.WriteAllBytes(FilePath, Encoder.EncodePreferences().ToArray());
            } catch (IOException) {}
        }

        static Preferences() {
            if (File.Exists(FilePath))
                using (FileStream file = File.Open(FilePath, FileMode.Open, FileAccess.Read))
                    Decoder.DecodeBlock(file, typeof(Preferences));

            Save();

            MIDI.DevicesUpdated += Save;
        }
    }
}