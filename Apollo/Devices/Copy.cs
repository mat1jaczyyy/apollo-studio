using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Copy: Device {
        public static readonly new string DeviceIdentifier = "copy";

        public enum CopyType {
            Static,
            Animate,
            Interpolate
        }

        public bool Mode; // true uses Length
        public Length Length;
        private int _rate;
        private decimal _gate;
        CopyType _copymode;
        public bool Loop;
        public List<Offset> Offsets;

        public int Rate {
            get => _rate;
            set {
                if (10 <= value && value <= 5000)
                    _rate = value;
            }
        }

        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4)
                    _gate = value;
            }
        }

        public string CopyMode {
            get => _copymode.ToString();
            set => _copymode = Enum.Parse<CopyType>(value);
        }

        public override Device Clone() => new Copy(Mode, Length, _rate, _gate, _copymode, Loop, Offsets);

        public Copy(bool mode = false, Length length = null, int rate = 500, decimal gate = 1, CopyType copymode = CopyType.Static, bool loop = false, List<Offset> offsets = null): base(DeviceIdentifier) {
            Mode = mode;
            Rate = rate;
            Length = length?? new Length();
            Gate = gate;
            _copymode = copymode;
            Loop = loop;
            Offsets = offsets?? new List<Offset>();
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            MIDIExit?.Invoke((Signal)courier.Info);
        }

        public override void MIDIEnter(Signal n) {
            int ox = n.Index % 10;
            int oy = n.Index / 10;

            for (int i = 0; i < Offsets.Count; i++) {
                int x = ox + Offsets[i].X;
                int y = oy + Offsets[i].Y;

                if (Loop) {
                    x = (x + 10) % 10;
                    y = (y + 10) % 10;
                }

                int result = y * 10 + x;
                    
                if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 99 && result != 9 && result != 90) {
                    Signal m = n.Clone();
                    m.Index = (byte)result;

                    if (_copymode == CopyType.Static) {
                        MIDIExit?.Invoke(m);

                    } else if (_copymode == CopyType.Animate) {
                        Courier courier = new Courier() {
                            Info = m,
                            AutoReset = false,
                            Interval = Convert.ToInt32((Mode? (int)Length : _rate) * _gate * (i + 1)),
                        };
                        courier.Elapsed += Tick;
                        courier.Start();
                    
                    } else if (_copymode == CopyType.Interpolate) {

                    }
                }
            }

            MIDIExit?.Invoke(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> offsets = JsonConvert.DeserializeObject<List<object>>(data["offsets"].ToString());
            List<Offset> initO = new List<Offset>();

            foreach (object offset in offsets)
                initO.Add(Offset.Decode(offset.ToString()));

            return new Copy(
                Convert.ToBoolean(data["mode"]),
                Length.Decode(data["length"].ToString()),
                Convert.ToInt32(data["rate"]),
                Convert.ToDecimal(data["gate"]),
                Enum.Parse<CopyType>(data["copymode"].ToString()),
                Convert.ToBoolean(data["loop"]),
                initO
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

                        writer.WritePropertyName("rate");
                        writer.WriteValue(_rate);

                        writer.WritePropertyName("gate");
                        writer.WriteValue(_gate);

                        writer.WritePropertyName("copymode");
                        writer.WriteValue(_copymode);

                        writer.WritePropertyName("loop");
                        writer.WriteValue(Loop);

                        writer.WritePropertyName("offsets");
                        writer.WriteStartArray();

                            for (int i = 0; i < Offsets.Count; i++)
                                writer.WriteRawValue(Offsets[i].Encode());

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}