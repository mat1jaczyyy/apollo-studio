using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Interfaces;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Devices {
    public class Pattern: Device, ISelectParent {
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

        List<Frame> _frames;
        public List<Frame> Frames {
            get => _frames;
            set {
                _frames = value;
                Reroute();
            }
        }

        void Reroute() {
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

        public void Remove(int index, bool dispose = true) {
            Window?.Contents_Remove(index);

            if (dispose) Frames[index].Dispose();
            Frames.RemoveAt(index);
            Reroute();

            Window?.Frame_Select(Expanded);
            Window?.Selection.Select(Frames[Expanded]);
        }

        ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        ConcurrentDictionary<Signal, List<Courier>> timers = new ConcurrentDictionary<Signal, List<Courier>>();
        ConcurrentHashSet<PolyInfo> poly = new ConcurrentHashSet<PolyInfo>();

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    
                    Window?.SetGate(Gate);
                }
            }
        }

        double _pinch;
        public double Pinch {
            get => _pinch;
            set {
                if (-2 <= value && value <= 2) {
                    _pinch = value;
                    
                    Window?.SetPinch(_pinch);
                }
            }
        }

        bool _bilateral;
        public bool Bilateral {
            get => _bilateral;
            set {
                if (_bilateral != value) {
                    _bilateral = value;

                    Window?.SetBilateral(_bilateral);
                }
            }
        }

        public double ApplyPinch(double time) => Pincher.ApplyPinch(
            time,
            Enumerable.Sum(Frames.Select(i => (double)i.Time)) * AdjustedRepeats * Gate,
            Pinch,
            Bilateral
        );

        class PolyInfo {
            public Signal n;
            public int index = 0;
            public object locker = new object();
            public List<Courier> timers = new List<Courier>();

            public PolyInfo(Signal init) => n = init;
        }

        PlaybackType _mode;
        public PlaybackType Mode {
            get => _mode;
            set {
                _mode = value;

                Window?.SetPlaybackMode(Mode);

                Stop();
            }
        }

        bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                if (_infinite != value) {
                    _infinite = value;

                    Window?.SetInfinite(_infinite);
                }
            }
        }

        int _repeats;
        public int Repeats {
            get => _repeats;
            set {
                if (_repeats != value && 1 <= value && value <= 128) {
                    _repeats = value;

                    Window?.SetRepeats(_repeats);
                }
            }
        }
        public int AdjustedRepeats => (Mode == PlaybackType.Loop || _infinite)? 1 : Repeats;

        int? _root;
        public int? RootKey {
            get => _root;
            set {
                if (_root != value && value != 100) {
                    _root = value;

                    Window?.SetRootKey(_root);
                }
            }
        }

        bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                Window?.SetWrap(Wrap);
            }
        }
        
        public override Device Clone() => new Pattern(Repeats, Gate, Pinch, Bilateral, (from i in Frames select i.Clone()).ToList(), Mode, Infinite, RootKey, Wrap, Expanded) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        int _expanded;
        public int Expanded {
            get => _expanded;
            set {
                if (!(0 <= value && value < Frames.Count)) value = 0;
                _expanded = value;                
            }
        }

        public Pattern(int repeats = 1, double gate = 1, double pinch = 0, bool bilateral = false, List<Frame> frames = null, PlaybackType mode = PlaybackType.Mono, bool infinite = false, int? root = null, bool wrap = false, int expanded = 0): base("pattern") {
            if (frames == null || frames.Count == 0) frames = new List<Frame>() {new Frame()};

            Repeats = repeats;
            Gate = gate;
            Pinch = pinch;
            Bilateral = bilateral;
            Frames = frames;
            Mode = mode;
            Infinite = infinite;
            RootKey = root;
            Wrap = wrap;
            Expanded = expanded;

            Reroute();
        }

        bool ApplyRootKey(int index, int trigger, out int result) {
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
                result = 100;
                return true;
            }
             
            return false;
        }

        void FireCourier(Signal n, double time) {
            Courier courier;

            timers[n].Add(courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        void FireCourier(PolyInfo info, double time) {
            Courier courier;

            info.timers.Add(courier = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            if (courier.Info is Signal n) {
                lock (locker[n]) {
                    if (++buffer[n] < Frames.Count * AdjustedRepeats) {
                        for (int i = 0; i < Frames[buffer[n] % Frames.Count].Screen.Length; i++)
                            if (Frames[buffer[n] % Frames.Count].Screen[i] != Frames[(buffer[n] - 1) % Frames.Count].Screen[i] && ApplyRootKey(i, n.Index, out int index))
                                    InvokeExit(n.With((byte)index, Frames[buffer[n] % Frames.Count].Screen[i].Clone()));

                    } else if (Mode == PlaybackType.Mono) {
                        if (!Infinite)
                            for (int i = 0; i < Frames.Last().Screen.Length; i++)
                                if (Frames.Last().Screen[i].Lit && ApplyRootKey(i, n.Index, out int index))
                                    InvokeExit(n.With((byte)index, new Color(0)));

                    } else if (Mode == PlaybackType.Loop) {
                        for (int i = 0; i < Frames[0].Screen.Length; i++)
                            if ((Infinite? Frames[0].Screen[i].Lit : Frames[0].Screen[i] != Frames[(buffer[n] - 1) % Frames.Count].Screen[i]) && ApplyRootKey(i, n.Index, out int index))
                                InvokeExit(n.With((byte)index, Frames[0].Screen[i].Clone()));

                        buffer[n] = 0;
                        double time = 0;

                        for (int i = 0; i < Frames.Count * AdjustedRepeats; i++) {
                            time += Frames[i % Frames.Count].Time * _gate;
                            FireCourier(n, time);
                        }
                    }

                    timers[n].Remove(courier);
                }

            } else if (courier.Info is PolyInfo info) {
                lock (info.locker) {
                    if (++info.index < Frames.Count * AdjustedRepeats) {
                        for (int i = 0; i < Frames[info.index % Frames.Count].Screen.Length; i++)
                            if (Frames[info.index % Frames.Count].Screen[i] != Frames[(info.index - 1) % Frames.Count].Screen[i] && ApplyRootKey(i, info.n.Index, out int index))
                                InvokeExit(info.n.With((byte)index, Frames[info.index % Frames.Count].Screen[i].Clone()));
                    } else {
                        poly.Remove(info);

                        if (!Infinite)
                            for (int i = 0; i < Frames.Last().Screen.Length; i++)
                                if (Frames.Last().Screen[i].Lit && ApplyRootKey(i, info.n.Index, out int index))
                                    InvokeExit(info.n.With((byte)index, new Color(0)));
                    }
                }
            }
        }

        void Stop(Signal n) {
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
                                InvokeExit(n.With((byte)index, new Color(0)));
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
                                InvokeExit(n.With((byte)index, Frames[0].Screen[i].Clone()));
                        
                        double time = 0;

                        PolyInfo info = new PolyInfo(n);
                        poly.Add(info);

                        for (int i = 0; i < Frames.Count * AdjustedRepeats; i++) {
                            time += Frames[i % Frames.Count].Time * _gate;
                            double pinched = ApplyPinch(time);

                            if (Mode == PlaybackType.Poly) FireCourier(info, pinched);
                            else FireCourier(n, pinched);
                        }
                    }
                }
            }
        }

        protected override void Stop() {
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

            buffer.Clear();
            locker.Clear();
        }

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            Window?.Close();
            Window = null;

            foreach (Frame frame in Frames) frame.Dispose();
            base.Dispose();
        }
    }
}