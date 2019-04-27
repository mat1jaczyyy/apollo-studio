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
        
        public List<Frame> Frames;

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4)
                    _gate = value;
            }
        }
        
        public override Device Clone() => new Pattern(Gate, (from i in Frames select i.Clone()).ToList(), Expanded);

        public int Expanded;

        public Pattern(decimal gate = 1, List<Frame> frames = null, int expanded = 0): base(DeviceIdentifier) {
            if (frames == null || frames.Count == 0) frames = new List<Frame>() {new Frame()};

            Gate = gate;
            Frames = frames;
            Expanded = expanded;
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
            if (Frames.Count > 0 && n.Color.Lit) {
                for (int i = 0; i < Frames[0].Screen.Length; i++)
                    if (Frames[0].Screen[i].Lit)
                        MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[0].Screen[i].Clone(), n.Layer, n.MultiTarget));
                
                decimal time = (Frames[0].Mode? (int)Frames[0].Length : Frames[0].Time) * _gate;

                for (int i = 1; i < Frames.Count; i++) {
                    for (int j = 0; j < Frames[i].Screen.Length; j++)
                        if (Frames[i].Screen[j] != Frames[i - 1].Screen[j])
                            FireCourier(n, Frames[i].Screen[j].Clone(), (byte)j, (int)time);

                    time += (Frames[i].Mode? (int)Frames[i].Length : Frames[i].Time) * _gate;
                }
                
                for (int i = 0; i < Frames.Last().Screen.Length; i++)
                    if (Frames.Last().Screen[i].Lit)
                        FireCourier(n, new Color(0), (byte)i, (int)time);
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
            
            List<object> _frames = JsonConvert.DeserializeObject<List<object>>(data["frames"].ToString());
            List<Frame> init = new List<Frame>();

            foreach (object frame in _frames)
                init.Add(Frame.Decode(frame.ToString()));

            return new Pattern(
                Convert.ToDecimal(data["gate"]),
                init,
                Convert.ToInt32(data["expanded"])
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

                        writer.WritePropertyName("gate");
                        writer.WriteValue(Gate);

                        writer.WritePropertyName("frames");
                        writer.WriteStartArray();

                            for (int i = 0; i < Frames.Count; i++)
                                writer.WriteRawValue(Frames[i].Encode());

                        writer.WriteEndArray();

                        writer.WritePropertyName("expanded");
                        writer.WriteValue(Expanded);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}