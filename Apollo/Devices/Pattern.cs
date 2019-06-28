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
        private HashSet<PolyInfo> poly = new HashSet<PolyInfo>();

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
            public List<Courier> timers = new List<Courier>();

            public PolyInfo(Signal init) => n = init;
        }

        public enum PlaybackType {
            Mono,
            Poly,
            Loop
        }

        private PlaybackType _mode;
        public PlaybackType Mode {
            get => _mode;
            set {
                _mode = value;

                Window?.SetPlaybackMode(Mode);

                Stop();
            }
        }

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

        private int _repeats;
        public int Repeats {
            get => _repeats;
            set {
                if (_repeats != value && 1 <= value && value <= 32) {
                    _repeats = value;

                    Window?.SetRepeats(_repeats);
                }
            }
        }
        private int AdjustedRepeats => (Mode == PlaybackType.Loop || _infinite)? 1 : Repeats;

        private int? _root;
        public int? RootKey {
            get => _root;
            set {
                if (_root != value) {
                    _root = value;

                    Window?.SetRootKey(_root);
                }
            }
        }

        private bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                Window?.SetWrap(Wrap);
            }
        }
        
        public override Device Clone() => new Pattern(Repeats, Gate, (from i in Frames select i.Clone()).ToList(), Mode, Infinite, RootKey, Wrap, Expanded) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public int Expanded;

        public Pattern(int repeats = 1, decimal gate = 1, List<Frame> frames = null, PlaybackType mode = PlaybackType.Mono, bool infinite = false, int? root = null, bool wrap = false, int expanded = 0): base(DeviceIdentifier) {
            if (frames == null || frames.Count == 0) frames = new List<Frame>() {new Frame()};

            Repeats = repeats;
            Gate = gate;
            Frames = frames;
            Mode = mode;
            Infinite = infinite;
            RootKey = root;
            Expanded = expanded;

            Reroute();
        }

        private bool ApplyRootKey(int index, int trigger, out int result) {
            if (RootKey == null) {
                result = index;
                return true;
            }

            int x = index % 10 + trigger % 10 - RootKey.Value % 10;
            int y = index / 10 + trigger / 10 - RootKey.Value / 10;

            if (Wrap) {
                x = (x + 10) % 10;
                y = (y + 10) % 10;
            }

            result = y * 10 + x;

            if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 98 && result != 9 && result != 90)
                return true;
            
            if (y == -1 && 4 <= x && x <= 5) {
                result = 99;
                return true;
            }
             
            return false;
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
            Courier courier;

            info.timers.Add(courier = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = (double)time,
            });
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
                    if (++buffer[n] < Frames.Count * AdjustedRepeats) {
                        for (int i = 0; i < Frames[buffer[n] % Frames.Count].Screen.Length; i++)
                            if (Frames[buffer[n] % Frames.Count].Screen[i] != Frames[(buffer[n] - 1) % Frames.Count].Screen[i] && ApplyRootKey(i, n.Index, out int index))
                                    MIDIExit?.Invoke(new Signal(n.Source, (byte)index, Frames[buffer[n] % Frames.Count].Screen[i].Clone(), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                    } else if (Mode == PlaybackType.Mono) {
                        if (!Infinite)
                            for (int i = 0; i < Frames.Last().Screen.Length; i++)
                                if (Frames.Last().Screen[i].Lit && ApplyRootKey(i, n.Index, out int index))
                                    MIDIExit?.Invoke(new Signal(n.Source, (byte)index, new Color(0), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                    } else if (Mode == PlaybackType.Loop) {
                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if ((Infinite? Frames[0].Screen[i].Lit : Frames[0].Screen[i] != Frames[(buffer[n] - 1) % Frames.Count].Screen[i]) && ApplyRootKey(i, n.Index, out int index))
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)index, Frames[0].Screen[i].Clone(), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));

                        buffer[n] = 0;
                        decimal time = 0;

                        for (int i = 0; i < Frames.Count * AdjustedRepeats; i++) {
                            time += Frames[i % Frames.Count].Time * _gate;
                            FireCourier(n, time);
                        }
                    }

                    timers[n].Remove(courier);
                }

            } else if (infoType == typeof(PolyInfo)) {
                PolyInfo info = (PolyInfo)courier.Info;
                
                lock (info.locker) {
                    if (++info.index < Frames.Count * AdjustedRepeats) {
                        for (int i = 0; i < Frames[info.index % Frames.Count].Screen.Length; i++)
                            if (Frames[info.index % Frames.Count].Screen[i] != Frames[(info.index - 1) % Frames.Count].Screen[i] && ApplyRootKey(i, info.n.Index, out int index))
                                MIDIExit?.Invoke(new Signal(info.n.Source, (byte)index, Frames[info.index % Frames.Count].Screen[i].Clone(), info.n.Page, info.n.Layer, info.n.BlendingMode, info.n.MultiTarget));
                    } else {
                        poly.Remove(info);

                        if (!Infinite)
                            for (int i = 0; i < Frames.Last().Screen.Length; i++)
                                if (Frames.Last().Screen[i].Lit && ApplyRootKey(i, info.n.Index, out int index))
                                    MIDIExit?.Invoke(new Signal(info.n.Source, (byte)index, new Color(0), info.n.Page, info.n.Layer, info.n.BlendingMode, info.n.MultiTarget));
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
                
                if (buffer.ContainsKey(n)) {
                    if (buffer[n] < Frames.Count * AdjustedRepeats - Convert.ToInt32(Infinite)) {
                        int originalIndex = buffer.Keys.First(x => x == n).Index;

                        for (int i = 0; i < Frames[buffer[n] % Frames.Count].Screen.Length; i++)
                            if (Frames[buffer[n] % Frames.Count].Screen[i].Lit && ApplyRootKey(i, originalIndex, out int index))
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)index, new Color(0), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));
                    }

                    buffer.Remove(n, out int _);
                }
                    

                timers[n] = new List<Courier>();
                buffer[n] = 0;
            }
        }

        public override void MIDIProcess(Signal n) {
            if (Frames.Count > 0) {
                bool lit = n.Color.Lit;
                n.HashIndex = false;
                n.Color = new Color();

                if (!locker.ContainsKey(n)) locker[n] = new object();

                lock (locker[n]) {
                    if ((Mode == PlaybackType.Mono && lit) || Mode == PlaybackType.Loop) Stop(n);

                    if (lit) {
                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if (Frames[0].Screen[i].Lit && ApplyRootKey(i, n.Index, out int index))
                                MIDIExit?.Invoke(new Signal(n.Source, (byte)index, Frames[0].Screen[i].Clone(), n.Page, n.Layer, n.BlendingMode, n.MultiTarget));
                        
                        decimal time = 0;
                        PolyInfo info = new PolyInfo(n);
                        poly.Add(info);

                        for (int i = 0; i < Frames.Count * AdjustedRepeats; i++) {
                            time += Frames[i % Frames.Count].Time * _gate;
                            if (Mode == PlaybackType.Poly) FireCourier(info, time);
                            else FireCourier(n, time);
                        }
                    }
                }
            }
        }

        protected override void Stop() {
            buffer.Clear();
            locker.Clear();
            
            foreach (List<Courier> i in timers.Values) {
                foreach (Courier j in i) j.Dispose();
                i.Clear();
            }
            timers.Clear();

            foreach (PolyInfo info in poly) {
                foreach (Courier i in info.timers) i.Dispose();
                info.timers.Clear();
            }
            poly.Clear();
        }

        public override void Dispose() {
            Stop();

            Window?.Close();
            Window = null;

            foreach (Frame frame in Frames) frame.Dispose();
            base.Dispose();
        }
    }
}