using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Preview: Device {
        public static readonly new string DeviceIdentifier = "preview";

        private Pixel[] screen = new Pixel[100];

        public override Device Clone() => new Preview();

        public Preview(): base(DeviceIdentifier) {
            for (int i = 0; i < 100; i++)
                screen[i] = new Pixel() {MIDIExit = PreviewExit};
        }

        public delegate void SignalExitedEventHandler(Signal n);
        public event SignalExitedEventHandler SignalExited;

        public void PreviewExit(Signal n) => SignalExited?.Invoke(n);

        public override void MIDIEnter(Signal n) {
            Signal m = n.Clone();

            MIDIExit?.Invoke(n);

            screen[m.Index].MIDIEnter(m);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            // Preview device has no data to parse

            return new Preview();
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();
                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}