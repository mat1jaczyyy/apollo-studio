using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.Elements;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Devices {
    public class Pattern: Device {
        public static readonly new string DeviceIdentifier = "pattern";

        public delegate void ChokedEventHandler(Pattern sender, int index);
        public static event ChokedEventHandler Choked;

        public PatternWindow Window;

        public List<Frame> Frames;

        private ConcurrentDictionary<Signal, int> _indexes = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        private ConcurrentDictionary<Signal, List<Courier>> _timers = new ConcurrentDictionary<Signal, List<Courier>>();
        private HashSet<PolyInfo> poly = new HashSet<PolyInfo>();

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

        public PlaybackType GetPlaybackType() => _mode;

        public int? Choke;
        bool choked;
        
        public override Device Clone() => new Pattern(Gate, (from i in Frames select i.Clone()).ToList(), _mode, Choke, Expanded);

        public int Expanded;

        public Pattern(decimal gate = 1, List<Frame> frames = null, PlaybackType mode = PlaybackType.Mono, int? choke = null, int expanded = 0): base(DeviceIdentifier) {
            if (frames == null || frames.Count == 0) frames = new List<Frame>() {new Frame()};

            Gate = gate;
            Frames = frames;
            _mode = mode;
            Choke = choke;
            Expanded = expanded;

            Choked += HandleChoke;
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
            if (choked) return;

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
                    } else {
                        for (int i = 0; i < Frames.Last().Screen.Length; i++)
                            if (Frames.Last().Screen[i].Lit)
                                MIDIExit?.Invoke(new Signal(info.n.Source, (byte)i, new Color(0), info.n.Layer, info.n.MultiTarget));
                        
                        poly.Remove(info);
                    }
                }
            }
        }

        private void Stop(Signal n) {
            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (_timers.ContainsKey(n))
                    for (int i = 0; i < _timers[n].Count; i++)
                        _timers[n][i].Dispose();
                
                if (_indexes.ContainsKey(n) && _indexes[n] < Frames.Count)
                    for (int i = 0; i < Frames[_indexes[n]].Screen.Length; i++)
                        if (Frames[_indexes[n]].Screen[i].Lit)
                            MIDIExit?.Invoke(new Signal(n.Source, (byte)i, new Color(0), n.Layer, n.MultiTarget));

                _timers[n] = new List<Courier>();
                _indexes[n] = 0;
            }
        }

        public override void MIDIEnter(Signal n) {
            if (Frames.Count > 0) {
                bool lit = n.Color.Lit;
                n.Index = 11;
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if ((_mode == PlaybackType.Mono && lit) || _mode == PlaybackType.Loop) Stop(n);

                    if (lit) {
                        if (Choke != null) {
                            Choked.Invoke(this, Choke.Value);
                            choked = false;
                        }

                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if (Frames[0].Screen[i].Lit)
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[0].Screen[i].Clone(), n.Layer, n.MultiTarget));
                        
                        decimal time = 0;
                        PolyInfo info = new PolyInfo(n);
                        if (_mode == PlaybackType.Poly) poly.Add(info);

                        for (int i = 0; i < Frames.Count; i++) {
                            time += (Frames[i].Mode? (int)Frames[i].Length : Frames[i].Time) * _gate;
                            if (_mode == PlaybackType.Poly) FireCourier(info, time);
                            else FireCourier(n, time);
                        }
                    }
                }
            }
        }

        private void HandleChoke(Pattern sender, int index) {
            if (Choke == index && sender != this) {
                choked = true;

                foreach ((Signal n, List<Courier> i) in _timers)
                    if (i.Count > 0) Stop(n);
                
                foreach (PolyInfo info in poly) {
                    lock (locker[info.n]) {
                        if (info.index < Frames.Count)
                            for (int i = 0; i < Frames[info.index].Screen.Length; i++)
                                if (Frames[info.index].Screen[i].Lit)
                                    MIDIExit?.Invoke(new Signal(info.n.Source, (byte)i, new Color(0), info.n.Layer, info.n.MultiTarget));
                    }

                    poly = new HashSet<PolyInfo>();
                }
            }
        }

        public override void Dispose() {
            Window?.Close();
            Window = null;

            base.Dispose();
        }
    }
}