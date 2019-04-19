using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Devices {
    public class Pattern: Device {
        public static readonly new string DeviceIdentifier = "pattern";

        public PatternWindow Window;
        
        private List<Frame> _frames;
        
        public override Device Clone() => new Pattern();

        public Pattern(List<Frame> frames = null): base(DeviceIdentifier) {
            _frames = frames?? new List<Frame>() {new Frame()};
        }

        private void FireCourier(Signal n, Color color, byte index, int time) {
            Courier courier = new Courier() {
                Info = new Signal(n.Source, (byte)index, color, n.Layer, n.MultiTarget),
                AutoReset = false,
                Interval = time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIEnter(Signal n) {
            if (_frames.Count > 0) {
                for (int i = 0; i < _frames[0].Screen.Length; i++)
                    if (_frames[0].Screen[i].Lit)
                        MIDIExit?.Invoke(new Signal(n.Source, (byte)i, _frames[0].Screen[i].Clone(), n.Layer, n.MultiTarget));
                
                int time = _frames[0].Time;

                for (int i = 1; i < _frames.Count; i++) {
                    for (int j = 0; j < _frames[i].Screen.Length; j++)
                        if (_frames[i].Screen[j] != _frames[i - 1].Screen[j])
                            FireCourier(n, _frames[i].Screen[j].Clone(), (byte)j, time);

                    time += _frames[i].Time;
                }
                
                for (int i = 0; i < _frames[0].Screen.Length; i++)
                    if (_frames[0].Screen[i].Lit)
                        FireCourier(n, new Color(0), (byte)i, time);
            }
        }

        public override void Dispose() {
            Window?.Close();
            Window = null;

            base.Dispose();
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Pattern(
                
            );
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