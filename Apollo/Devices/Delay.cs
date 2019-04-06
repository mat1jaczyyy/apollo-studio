using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Delay: Device {
        public static readonly new string DeviceIdentifier = "delay";

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;
        private Decimal _gate;

        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public Decimal Gate {
            get => _gate;
            set {
                if (0 <= value && value <= 4)
                    _gate = value;
            }
        }

        public override Device Clone() => new Delay(Mode, Length, _time, _gate);

        public Delay(bool mode = false, Length length = null, int time = 1000, Decimal gate = 1): base(DeviceIdentifier) {
            if (length == null) length = new Length();

            Mode = mode;
            Time = time;
            Length = length;
            Gate = gate;
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIEnter(Signal n) {
            Courier courier = new Courier() {
                Info = n.Clone(),
                AutoReset = false,
                Interval = Convert.ToInt32((Mode? (int)Length : _time) * _gate),
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            return new Delay(
                Convert.ToBoolean(data["mode"]),
                Length.Decode(data["length"].ToString()),
                Convert.ToInt32(data["time"]),
                Convert.ToDecimal(data["gate"])
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
                        writer.WriteValue(Length.Encode());

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("gate");
                        writer.WriteValue(_gate);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}