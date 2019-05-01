using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Interpolate,
            RandomSingle,
            RandomLoop
        }
        
        private Random RNG = new Random();

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
            get {
                if (_copymode == CopyType.Static) return "Static";
                else if (_copymode == CopyType.Animate) return "Animate";
                else if (_copymode == CopyType.Interpolate) return "Interpolate";
                else if (_copymode == CopyType.RandomSingle) return "Random Single";
                else if (_copymode == CopyType.RandomLoop) return "Random Loop";
                return null;
            }
            set {
                if (value == "Static") _copymode = CopyType.Static;
                else if (value == "Animate") _copymode = CopyType.Animate;
                else if (value == "Interpolate") _copymode = CopyType.Interpolate;
                else if (value == "Random Single") _copymode = CopyType.RandomSingle;
                else if (value == "Random Loop") _copymode = CopyType.RandomLoop;
            }
        }

        private ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        private ConcurrentDictionary<Signal, Courier> timers = new ConcurrentDictionary<Signal, Courier>();

        public override Device Clone() => new Copy(Mode, Length.Clone(), _rate, _gate, _copymode, Loop, (from i in Offsets select i.Clone()).ToList());

        public Copy(bool mode = false, Length length = null, int rate = 500, decimal gate = 1, CopyType copymode = CopyType.Static, bool loop = false, List<Offset> offsets = null): base(DeviceIdentifier) {
            Mode = mode;
            Rate = rate;
            Length = length?? new Length();
            Gate = gate;
            _copymode = copymode;
            Loop = loop;
            Offsets = offsets?? new List<Offset>();
        }

        private void FireCourier(Signal n, decimal time) {
            Courier courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = (double)time
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void FireCourier((Signal n, List<int>) info, decimal time) {
            Courier courier = timers[info.n] = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = (double)time
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            Type infoType = courier.Info.GetType();
            
            if (infoType == typeof(Signal))
                MIDIExit?.Invoke((Signal)courier.Info);
            else if (infoType == typeof((Signal, List<int>))) {
                (Signal n, List<int> offsets) = ((Signal, List<int>))courier.Info;
                HandleRandomLoop(n, offsets);
            }
        }

        private void HandleRandomLoop(Signal original, List<int> offsets) {
            Signal n = original.Clone();
            Signal m = original.Clone();
            n.Color = new Color();

            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = RNG.Next(offsets.Count);
                    m.Index = (byte)offsets[buffer[n]];

                } else {
                    Signal o = original.Clone();
                    o.Index = (byte)offsets[buffer[n]];
                    o.Color = new Color(0);
                    MIDIExit?.Invoke(o);

                    if (m.Color.Lit) {
                        if (offsets.Count > 1) {
                            int old = buffer[n];
                            buffer[n] = RNG.Next(offsets.Count - 1);
                            if (buffer[n] >= old) buffer[n]++;
                        }
                        m.Index = (byte)offsets[buffer[n]];
                    
                    } else buffer.Remove(n, out int _);
                }

                if (buffer.ContainsKey(n)) {
                    MIDIExit?.Invoke(m);
                    FireCourier((original, offsets), (Mode? (int)Length : _rate) * _gate);
                } else {
                    timers[n].Dispose();
                    timers.Remove(n, out Courier _);
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            int ox = n.Index % 10;
            int oy = n.Index / 10;

            int px = ox;
            int py = oy;

            List<int> validOffsets = new List<int>() {n.Index};
            int time = 0;

            for (int i = 0; i < Offsets.Count; i++) {
                int x = ox + Offsets[i].X;
                int y = oy + Offsets[i].Y;

                if (Loop) {
                    x = (x + 10) % 10;
                    y = (y + 10) % 10;
                }

                int result = y * 10 + x;
                bool valid = true;
                
                if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 98 && result != 9 && result != 90)
                    validOffsets.Add(result);

                else if (y == -1 && 4 <= x && x <= 5)
                    validOffsets.Add(result = 99);

                else valid = false;

                if (valid)
                    if (_copymode == CopyType.Static) {
                        Signal m = n.Clone();
                        m.Index = (byte)result;

                        MIDIExit?.Invoke(m);

                    } else if (_copymode == CopyType.Animate) {
                        Signal m = n.Clone();
                        m.Index = (byte)result;

                        FireCourier(m, (Mode? (int)Length : _rate) * _gate * (i + 1));
                    }

                if (_copymode == CopyType.Interpolate) {
                    List<(int X, int Y)> points = new List<(int, int)>();

                    int dx = x - px;
                    int dy = y - py;

                    int ax = Math.Abs(dx);
                    int ay = Math.Abs(dy);

                    int bx = (dx < 0)? -1 : 1;
                    int by = (dy < 0)? -1 : 1;

                    if (ax > ay) for (int j = 1; j <= ax; j++)
                        points.Add((px + j * bx, py + (int)Math.Round((double)j / ax * ay) * by));

                    else for (int j = 1; j <= ay; j++)
                        points.Add((px + (int)Math.Round((double)j / ay * ax) * bx, py + j * by));
                    
                    foreach ((int ix, int iy) in points) {
                        int iresult = iy * 10 + ix;
                        time++;
                        
                        Signal m = n.Clone();
                        bool ivalid = true;
                        
                        if (0 <= ix && ix <= 9 && 0 <= iy && iy <= 9 && 1 <= iresult && iresult <= 98 && iresult != 9 && iresult != 90)
                            m.Index = (byte)(iy * 10 + ix);
                        
                        else if (iy == -1 && 4 <= ix && ix <= 5)
                            m.Index = 99;
                        
                        else ivalid = false;

                        if (ivalid) FireCourier(m, (Mode? (int)Length : _rate) * _gate * time);
                    }
                }

                px = x;
                py = y;
            }

            if (_copymode == CopyType.RandomSingle) {
                Signal m = n.Clone();
                n.Color = new Color();

                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = m.Index = (byte)validOffsets[RNG.Next(validOffsets.Count)];

                } else {
                    m.Index = (byte)buffer[n];
                    if (!m.Color.Lit) buffer.Remove(n, out int _);
                }

                MIDIExit?.Invoke(m);

            } else if (_copymode == CopyType.RandomLoop) HandleRandomLoop(n, validOffsets);
            
            else MIDIExit?.Invoke(n);
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