using System;
using System.Collections.Concurrent;
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

        private ConcurrentDictionary<Signal, int> _indexes = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        private ConcurrentDictionary<Signal, List<Courier>> _timers = new ConcurrentDictionary<Signal, List<Courier>>();

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4)
                    _gate = value;
            }
        }

        private class PolyInfo {
            public Signal n;
            public int index = 0;
            public object locker = new object();

            public PolyInfo(Signal init) => n = init;
        }

        public enum PlaybackType {
            Mono,
            Poly,
            Loop
        }

        private PlaybackType _mode;
        public string Mode {
            get => _mode.ToString();
            set => _mode = Enum.Parse<PlaybackType>(value);
        }
        
        public override Device Clone() => new Pattern(Gate, _mode, (from i in Frames select i.Clone()).ToList(), Expanded);

        public int Expanded;

        public Pattern(decimal gate = 1, PlaybackType mode = PlaybackType.Mono, List<Frame> frames = null, int expanded = 0): base(DeviceIdentifier) {
            if (frames == null || frames.Count == 0) frames = new List<Frame>() {new Frame()};

            Gate = gate;
            _mode = mode;
            Frames = frames;
            Expanded = expanded;
        }

        private void FireCourier(Signal n, decimal time) {
            Courier courier;

            _timers[n].Add(courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = (double)time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void FireCourier(PolyInfo info, decimal time) {
            Courier courier = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = (double)time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            Type infoType = courier.Info.GetType();
            
            if (infoType == typeof(Signal)) {
                Signal n = (Signal)courier.Info;

                lock (locker[n]) {
                    if (++_indexes[n] < Frames.Count) {
                        for (int i = 0; i < Frames[_indexes[n]].Screen.Length; i++)
                            if (Frames[_indexes[n]].Screen[i] != Frames[_indexes[n] - 1].Screen[i])
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[_indexes[n]].Screen[i].Clone(), n.Layer, n.MultiTarget));

                    } else if (_mode == PlaybackType.Mono) {
                        for (int i = 0; i < Frames.Last().Screen.Length; i++)
                            if (Frames.Last().Screen[i].Lit)
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, new Color(0), n.Layer, n.MultiTarget));

                    } else if (_mode == PlaybackType.Loop) {
                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if (Frames[0].Screen[i] != Frames[_indexes[n] - 1].Screen[i])
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[0].Screen[i].Clone(), n.Layer, n.MultiTarget));

                        _indexes[n] = 0;
                        decimal time = 0;

                        for (int i = 0; i < Frames.Count; i++) {
                            time += (Frames[i].Mode? (int)Frames[i].Length : Frames[i].Time) * _gate;
                            FireCourier(n, time);
                        }
                    }

                    _timers[n].Remove(courier);
                }

            } else if (infoType == typeof(PolyInfo)) {
                PolyInfo info = (PolyInfo)courier.Info;
                
                lock (info.locker) {
                    if (++info.index < Frames.Count) {
                        for (int i = 0; i < Frames[info.index].Screen.Length; i++)
                            if (Frames[info.index].Screen[i] != Frames[info.index - 1].Screen[i])
                                MIDIExit?.Invoke(new Signal(info.n.Source, (byte)i, Frames[info.index].Screen[i].Clone(), info.n.Layer, info.n.MultiTarget));
                    } else
                        for (int i = 0; i < Frames.Last().Screen.Length; i++)
                            if (Frames.Last().Screen[i].Lit)
                                MIDIExit?.Invoke(new Signal(info.n.Source, (byte)i, new Color(0), info.n.Layer, info.n.MultiTarget));
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (Frames.Count > 0 && n.Color.Lit) {
                n.Index = 11;
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();
                
                lock (locker[n]) {
                    if (_mode != PlaybackType.Poly) {
                        if (_timers.ContainsKey(n))
                            for (int i = 0; i < _timers[n].Count; i++)
                                _timers[n][i].Dispose();

                        _timers[n] = new List<Courier>();
                        _indexes[n] = 0;
                    }

                    for (int i = 0; i < Frames[0].Screen.Length; i++)
                        if (Frames[0].Screen[i].Lit)
                            MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[0].Screen[i].Clone(), n.Layer, n.MultiTarget));
                    
                    decimal time = 0;
                    PolyInfo info = new PolyInfo(n);

                    for (int i = 0; i < Frames.Count; i++) {
                        time += (Frames[i].Mode? (int)Frames[i].Length : Frames[i].Time) * _gate;
                        if (_mode == PlaybackType.Poly) FireCourier(info, time);
                        else FireCourier(n, time);
                    }
                }
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
                Convert.ToDecimal(data["gate"].ToString()),
                Enum.Parse<PlaybackType>(data["mode"].ToString()),
                init,
                Convert.ToInt32(data["expanded"].ToString())
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

                        writer.WritePropertyName("mode");
                        writer.WriteValue(_mode);

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