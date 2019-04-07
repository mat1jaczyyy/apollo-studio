using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Windows;

namespace Apollo.Core {
    public static class Preferences {
        public static readonly string Identifier = "preferences";

        public static PreferencesWindow Window;

        private static readonly string FilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Apollo.config.json";

        public delegate void CheckBoxChanged(bool NewValue);

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

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}