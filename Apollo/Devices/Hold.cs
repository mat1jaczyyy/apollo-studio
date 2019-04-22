using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Hold: Device {
        public static readonly new string DeviceIdentifier = "hold";

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;
        private decimal _gate;
        public bool Infinite;
        public bool Release;

        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4)
                    _gate = value;
            }
        }

        public override Device Clone() => new Hold(Mode, Length, _time, _gate, Infinite, Release);

        public Hold(bool mode = false, Length length = null, int time = 1000, decimal gate = 1, bool infinite = false, bool release = false): base(DeviceIdentifier) {
            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Gate = gate;
            Infinite = infinite;
            Release = release;
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit ^ Release) {
                if (!Infinite) {
                    Courier courier = new Courier() {
                        Info = new Signal(Track.Get(this).Launchpad, n.Index, new Color(0), n.Layer),
                        AutoReset = false,
                        Interval = Convert.ToInt32((Mode? (int)Length : _time) * _gate),
                    };
                    courier.Elapsed += Tick;
                    courier.Start();
                }

                if (Release) n.Color = new Color();
                MIDIExit?.Invoke(n);
            }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Hold(
                Convert.ToBoolean(data["mode"]),
                Length.Decode(data["length"].ToString()),
                Convert.ToInt32(data["time"]),
                Convert.ToDecimal(data["gate"]),
                Convert.ToBoolean(data["infinite"])
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

                        writer.WritePropertyName("mode");
                        writer.WriteValue(Mode);

                        writer.WritePropertyName("length");
                        writer.WriteRawValue(Length.Encode());

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("gate");
                        writer.WriteValue(_gate);

                        writer.WritePropertyName("infinite");
                        writer.WriteValue(Infinite);

                        writer.WritePropertyName("release");
                        writer.WriteValue(Release);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}