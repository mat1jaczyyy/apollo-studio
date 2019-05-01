using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Components;
using Apollo.Windows;

namespace Apollo.Core {
    public static class Preferences {
        public static readonly string Identifier = "preferences";

        public static PreferencesWindow Window;

        private static readonly string FilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Apollo.config.json";

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

        public static void Save() => File.WriteAllText(FilePath, Encode());

        static Preferences() {
            if (!(File.Exists(FilePath) && Decode(File.ReadAllText(FilePath)))) Save();
        }
        
        private static bool Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return false;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            try {
                AlwaysOnTop = Convert.ToBoolean(data["alwaysontop"]);
                CenterTrackContents = Convert.ToBoolean(data["centertrackcontents"]);
                AutoCreateKeyFilter = Convert.ToBoolean(data["autocreatekeyfilter"]);
                AutoCreatePageFilter = Convert.ToBoolean(data["autocreatepagefilter"]);
                FadeSmoothness = Convert.ToDouble(data["fadesmoothness"]);
                ColorHistory.Decode(data["colorhistory"].ToString());
                CopyPreviousFrame = Convert.ToBoolean(data["copypreviousframe"]);
            } catch {
                return false;
            }

            return true;
        }

        private static string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("alwaysontop");
                        writer.WriteValue(AlwaysOnTop);

                        writer.WritePropertyName("centertrackcontents");
                        writer.WriteValue(CenterTrackContents);

                        writer.WritePropertyName("autocreatekeyfilter");
                        writer.WriteValue(AutoCreateKeyFilter);

                        writer.WritePropertyName("autocreatepagefilter");
                        writer.WriteValue(AutoCreatePageFilter);

                        writer.WritePropertyName("fadesmoothness");
                        writer.WriteValue(FadeSmoothnessSlider);

                        writer.WritePropertyName("colorhistory");
                        writer.WriteRawValue(ColorHistory.Encode());

                        writer.WritePropertyName("copypreviousframe");
                        writer.WriteValue(CopyPreviousFrame);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}