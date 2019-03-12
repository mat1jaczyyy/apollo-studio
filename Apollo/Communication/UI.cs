using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Communication {
    public class UI {
        private static readonly string ip = "localhost";
        private static readonly ushort port = 1549;

        public static string EncodeMessage(string recipient, string type, Dictionary<string, object> content, string device = "") {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("message");

                    writer.WritePropertyName("recipient");
                    writer.WriteValue(recipient);

                    if (recipient == "device") {
                        writer.WritePropertyName("device");
                        writer.WriteValue(device);
                    }

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("type");
                        writer.WriteValue(type);

                        foreach (KeyValuePair<string, object> entry in content) {
                            writer.WritePropertyName(entry.Key);
                            string value = entry.Value.ToString();

                            if (value[0].Equals('{') || value[0].Equals('['))
                                writer.WriteRawValue(value);
                            else
                                writer.WriteValue(value);
                        }

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }

            return json.ToString();
        }

        public static async void Init() {
            using (HttpClient client = new HttpClient())
                await client.PostAsync($"http://{ip}:{port}/init", new StringContent(Set.Encode(), Encoding.UTF8, "application/json"));
        }

        public static async void App(string json) {
            Program.Log($"UI  ** {json}");
            using (HttpClient client = new HttpClient())
                await client.PostAsync($"http://{ip}:{port}/app", new StringContent(json, Encoding.UTF8, "application/json"));
        }
    }
}