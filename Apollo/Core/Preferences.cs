using System;
using System.IO;

using Apollo.Binary;
using Apollo.Windows;

namespace Apollo.Core {
    public static class Preferences {
        public static PreferencesWindow Window;

        private static readonly string FilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Apollo.config";

        public delegate void CheckBoxChanged(bool newValue);
        public delegate void SmoothnessChanged(double newValue);

        public static event CheckBoxChanged AlwaysOnTopChanged;
        private static bool _AlwaysOnTop = true;
        public static bool AlwaysOnTop {
            get => _AlwaysOnTop;
            set {
                _AlwaysOnTop = value;
                AlwaysOnTopChanged?.Invoke(_AlwaysOnTop);
                Save();
            }
        }

        public static event CheckBoxChanged CenterTrackContentsChanged;
        private static bool _CenterTrackContents = true;
        public static bool CenterTrackContents {
            get => _CenterTrackContents;
            set {
                _CenterTrackContents = value;
                CenterTrackContentsChanged?.Invoke(_CenterTrackContents);
                Save();
            }
        }

        private static bool _AutoCreateKeyFilter = true;
        public static bool AutoCreateKeyFilter {
            get => _AutoCreateKeyFilter;
            set {
                _AutoCreateKeyFilter = value;
                Save();
            }
        }

        private static bool _AutoCreatePageFilter = false;
        public static bool AutoCreatePageFilter {
            get => _AutoCreatePageFilter;
            set {
                _AutoCreatePageFilter = value;
                Save();
            }
        }

        public static event SmoothnessChanged FadeSmoothnessChanged;
        public static double FadeSmoothnessSlider { get; private set; } = 1;
        private static double _FadeSmoothness;
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

        private static bool _CopyPreviousFrame = false;
        public static bool CopyPreviousFrame {
            get => _CopyPreviousFrame;
            set {
                _CopyPreviousFrame = value;
                Save();
            }
        }

        private static bool _EnableGestures = false;
        public static bool EnableGestures {
            get => _EnableGestures;
            set {
                _EnableGestures = value;
                Save();
            }
        }

        private static bool _DiscordPresence = true;
        public static bool DiscordPresence {
            get => _DiscordPresence;
            set {
                _DiscordPresence = value;
                Discord.Set(DiscordPresence);
                Save();
            }
        }

        private static bool _DiscordFilename = false;
        public static bool DiscordFilename {
            get => _DiscordFilename;
            set {
                _DiscordFilename = value;
                Discord.Set(DiscordPresence);
                Save();
            }
        }

        public static void Save() {
            try {
                File.WriteAllBytes(FilePath, Encoder.EncodePreferences().ToArray());
            } catch (IOException) {}
        } 

        static Preferences() {
            if (File.Exists(FilePath)) 
                using (FileStream file = File.Open(FilePath, FileMode.Open))
                    Decoder.Decode(file, typeof(Preferences));

            Save();
        }
    }
}