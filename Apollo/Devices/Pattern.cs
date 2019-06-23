using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Devices {
    public class Pattern: Device, ISelectParent {
        public static readonly new string DeviceIdentifier = "pattern";        

        public ISelectParentViewer IViewer {
            get => Window;
        }

        public List<ISelect> IChildren {
            get => Frames.Select(i => (ISelect)i).ToList();
        }

        public bool IRoot {
            get => true;
        }

        public PatternWindow Window;

        private List<Frame> _frames;
        public List<Frame> Frames {
            get => _frames;
            set {
                _frames = value;
                Reroute();
            }
        }

        private void Reroute() {
            for (int i = 0; i < Frames.Count; i++) {
                Frames[i].Parent = this;
                Frames[i].ParentIndex = i;
            }
            
            Window?.SetInfinite(Infinite);
        }

        public Frame this[int index] {
            get => Frames[index];
        }

        public int Count {
            get => Frames.Count;
        }
        
        public void Insert(int index, Frame frame) {
            Frames.Insert(index, frame);
            Reroute();

            Window?.Contents_Insert(index, Frames[index]);
            
            Window?.Selection.Select(frame);
            Window?.Frame_Select(index);
        }

        public void Remove(int index) {
            Window?.Contents_Remove(index);

            Frames.RemoveAt(index);
            Reroute();

            Window?.Frame_Select(Expanded);
            Window?.Selection.Select(Frames[Expanded]);
        }

        private ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        private ConcurrentDictionary<Signal, List<Courier>> timers = new ConcurrentDictionary<Signal, List<Courier>>();

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    
                    Window?.SetGate(Gate);
                }
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
            set {
                _mode = Enum.Parse<PlaybackType>(value);

                Window?.SetPlaybackMode(Mode);
            }
        }

        public PlaybackType GetPlaybackType() => _mode;

        private bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                if (_infinite != value) {
                    _infinite = value;

                    Window?.SetInfinite(_infinite);
                }
            }
        }
        
        public override Device Clone() => new Pattern(Gate, (from i in Frames select i.Clone()).ToList(), _mode, Infinite, Expanded) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public int Expanded;

        public Pattern(decimal gate = 1, List<Frame> frames = null, PlaybackType mode = PlaybackType.Mono, bool infinite = false, int expanded = 0): base(DeviceIdentifier) {
            if (frames == null || frames.Count == 0) frames = new List<Frame>() {new Frame()};

            Gate = gate;
            Frames = frames;
            _mode = mode;
            Infinite = infinite;
            Expanded = expanded;

            Reroute();
        }

        private void FireCourier(Signal n, decimal time) {
            Courier courier;

            timers[n].Add(courier = new Courier() {
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
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            Type infoType = courier.Info.GetType();
            
            if (infoType == typeof(Signal)) {
                Signal n = (Signal)courier.Info;

                lock (locker[n]) {
                    if (++buffer[n] < Frames.Count) {
                        for (int i = 0; i < Frames[buffer[n]].Screen.Length; i++)
                            if (Frames[buffer[n]].Screen[i] != Frames[buffer[n] - 1].Screen[i])
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[buffer[n]].Screen[i].Clone(), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                    } else if (_mode == PlaybackType.Mono) {
                        if (!Infinite)
                            for (int i = 0; i < Frames.Last().Screen.Length; i++)
                                if (Frames.Last().Screen[i].Lit)
                                    MIDIExit?.Invoke(new Signal(n.Source, (byte)i, new Color(0), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                    } else if (_mode == PlaybackType.Loop) {
                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if (Infinite? Frames[0].Screen[i].Lit : Frames[0].Screen[i] != Frames[buffer[n] - 1].Screen[i])
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[0].Screen[i].Clone(), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                        buffer[n] = 0;
                        decimal time = 0;

                        for (int i = 0; i < Frames.Count; i++) {
                            time += Frames[i].Time * _gate;
                            FireCourier(n, time);
                        }
                    }

                    timers[n].Remove(courier);
                }

            } else if (infoType == typeof(PolyInfo)) {
                PolyInfo info = (PolyInfo)courier.Info;
                
                lock (info.locker) {
                    if (++info.index < Frames.Count) {
                        for (int i = 0; i < Frames[info.index].Screen.Length; i++)
                            if (Frames[info.index].Screen[i] != Frames[info.index - 1].Screen[i])
                                MIDIExit?.Invoke(new Signal(info.n.Source, (byte)i, Frames[info.index].Screen[i].Clone(), info.n.Page, info.n.Layer, info.n.BlendingMode, info.n.MultiTarget));
                    } else {
                        if (!Infinite)
                            for (int i = 0; i < Frames.Last().Screen.Length; i++)
                                if (Frames.Last().Screen[i].Lit)
                                    MIDIExit?.Invoke(new Signal(info.n.Source, (byte)i, new Color(0), info.n.Page, info.n.Layer, info.n.BlendingMode, info.n.MultiTarget));
                    }
                }
            }
        }

        private void Stop(Signal n) {
            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (timers.ContainsKey(n))
                    for (int i = 0; i < timers[n].Count; i++)
                        timers[n][i].Dispose();
                
                if (buffer.ContainsKey(n) && buffer[n] < Frames.Count - Convert.ToInt32(Infinite))
                    for (int i = 0; i < Frames[buffer[n]].Screen.Length; i++)
                        if (Frames[buffer[n]].Screen[i].Lit)
                            MIDIExit?.Invoke(new Signal(n.Source, (byte)i, new Color(0), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                timers[n] = new List<Courier>();
                buffer[n] = 0;
            }
        }

        public override void MIDIProcess(Signal n) {
            if (Frames.Count > 0) {
                bool lit = n.Color.Lit;
                n.Index = 11;
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if ((_mode == PlaybackType.Mono && lit) || _mode == PlaybackType.Loop) Stop(n);

                    if (lit) {
                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if (Frames[0].Screen[i].Lit)
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)i, Frames[0].Screen[i].Clone(), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));
                        
                        decimal time = 0;
                        PolyInfo info = new PolyInfo(n);

                        for (int i = 0; i < Frames.Count; i++) {
                            time += Frames[i].Time * _gate;
                            if (_mode == PlaybackType.Poly) FireCourier(info, time);
                            else FireCourier(n, time);
                        }
                    }
                }
            }
        }

        public override void Dispose() {
            buffer.Clear();
            locker.Clear();
            
            foreach (List<Courier> i in timers.Values) {
                foreach (Courier j in i) j.Dispose();
                i.Clear();
            }
            timers.Clear();

            Window?.Close();
            Window = null;

            foreach (Frame frame in Frames) frame.Dispose();
            base.Dispose();
        }
    }
}