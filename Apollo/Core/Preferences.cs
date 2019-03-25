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

        public delegate void AlwaysOnTopChangedEventHandler(bool NewValue);
        public static event AlwaysOnTopChangedEventHandler AlwaysOnTopChanged;

        private static bool _AlwaysOnTop = true;
        public static bool AlwaysOnTop {
            get => _AlwaysOnTop;
            set {
                _AlwaysOnTop = value;
                AlwaysOnTopChanged?.Invoke(_AlwaysOnTop);
                Save();
            }
        }

        public delegate void CenterTrackContentsChangedEventHandler(bool NewValue);
        public static event CenterTrackContentsChangedEventHandler CenterTrackContentsChanged;

        private static bool _CenterTrackContents = true;
        public static bool CenterTrackContents {
            get => _CenterTrackContents;
            set {
                _CenterTrackContents = value;
                CenterTrackContentsChanged?.Invoke(_CenterTrackContents);
                Save();
            }
        }

        public static void Save() {
            File.WriteAllText(FilePath, Encode());
        }

        static Preferences() {
            if (File.Exists(FilePath)) {
                Decode(File.ReadAllText(FilePath));
            } else {
                Save();
            }
        }
        
        private static void Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            AlwaysOnTop = Convert.ToBoolean(data["alwaysontop"]);
            CenterTrackContents = Convert.ToBoolean(data["centertrackcontents"]);
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

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}