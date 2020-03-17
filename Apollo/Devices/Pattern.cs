using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Undo;
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

        public void IInsert(int index, ISelect item) => Insert(index, (Frame)item);

        public PatternWindow Window;

        List<Frame> _frames;
        public List<Frame> Frames {
            get => _frames;
            set {
                if (value != null && value.Count != 0) {
                    _frames = value;

                    Window?.RecreateFrames();

                    Reroute();
                }
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
        
        public class FrameInsertedUndoEntry: PathUndoEntry<Pattern> {
            int index;
            Frame frame;

            protected override void UndoPath(params Pattern[] items) => items[0].Remove(index);
            protected override void RedoPath(params Pattern[] items) => items[0].Insert(index, frame.Clone());
            
            protected override void DisposePath(params Pattern[] items) => frame.Dispose();
            
            public FrameInsertedUndoEntry(Pattern pattern, int index, Frame frame)
            : base($"Pattern Frame {index + 1} Inserted", pattern) {
                this.index = index;
                this.frame = frame.Clone();
            }
        }
        
        public class FrameChangedUndoEntry: SimpleIndexPathUndoEntry<Pattern, Color[]> {
            static Color[] Clone(Color[] arr) => (from i in arr select i.Clone()).ToArray();

            protected override void Action(Pattern item, int index, Color[] element) => item[index].Screen = Clone(element);
            
            public FrameChangedUndoEntry(Pattern pattern, int index, Color[] u)
            : base($"Pattern Frame {index + 1} Changed", pattern, index, Clone(u), Clone(pattern[index].Screen)) {}
        }
        
        public class InfiniteUndoEntry: SimplePathUndoEntry<Pattern, bool> {
            protected override void Action(Pattern item, bool element) => item.Infinite = element;
            
            public InfiniteUndoEntry(Pattern pattern, bool u, bool r)
            : base($"Pattern Infinite Changed to {(r? "Enabled" : "Disabled")}", pattern, u, r) {}
        }
        
        public abstract class DurationUndoEntry<I>: PathUndoEntry<Pattern> {
            int left;
            List<Time> u;
            I r;

            protected abstract void SetValue(Time item, I element);

            protected override void UndoPath(params Pattern[] items) {
                for (int i = 0; i < u.Count; i++)
                    items[0][left + i].Time = u[i].Clone();
            }

            protected override void RedoPath(params Pattern[] items) {
                for (int i = 0; i < u.Count; i++) {
                    SetValue(items[0][left + i].Time, r);
                }
            }
            
            protected override void DisposePath(params Pattern[] items) {
                foreach (Time time in u) time.Dispose();
                u = null;
            }
            
            public DurationUndoEntry(string desc, Pattern pattern, int left, List<Time> u, I r)
            : base(desc, pattern) {
                this.left = left;
                this.u = (from i in u select i.Clone()).ToList();
                this.r = r;
            }
        }

        public class DurationValueUndoEntry: DurationUndoEntry<int> {
            protected override void SetValue(Time item, int element) {
                item.Free = element;
                item.Mode = false;
            }

            public DurationValueUndoEntry(Pattern pattern, int left, List<Time> u, int r)
            : base($"Pattern Frame {pattern.Expanded + 1} Duration Changed to {r}ms", pattern, left, u, r) {}
        }

        public class DurationStepUndoEntry: DurationUndoEntry<int> {
            protected override void SetValue(Time item, int element) {
                item.Free = element;
                item.Mode = true;
            }

            public DurationStepUndoEntry(Pattern pattern, int left, List<Time> u, int r)
            : base($"Pattern Frame {pattern.Expanded + 1} Duration Changed to {Length.Steps[r]}", pattern, left, u, r) {}
        }

        public class DurationModeUndoEntry: DurationUndoEntry<bool> {
            protected override void SetValue(Time item, bool element) => item.Mode = element;

            public DurationModeUndoEntry(Pattern pattern, int left, List<Time> u, bool r)
            : base($"Pattern Frame {pattern.Expanded + 1} Duration Switched to {(r? "Steps" : "Free")}", pattern, left, u, r) {}
        }
        
        public class FrameReversedUndoEntry: SymmetricPathUndoEntry<Pattern> {
            int index, left, right;

            protected override void Action(Pattern item) {
                if (item.Window != null) item.Window.Draw = false;

                for (int i = left; i < right; i++) {
                    Frame frame = item[right];
                    item.Remove(right, false);
                    item.Insert(i, frame);
                }

                if (item.Window != null) {
                    item.Window.Draw = true;

                    item.Window.Frame_Select(index);
                    item.Window.Selection.Select(item[left]);
                    item.Window.Selection.Select(item[right], true);
                }
            }
            
            public FrameReversedUndoEntry(Pattern pattern, int index, int left, int right)
            : base($"Pattern Frames Reversed", pattern) {
                this.index = index;
                this.left = left;
                this.right = right;
            }
        }
        
        public class FrameInvertedUndoEntry: SymmetricPathUndoEntry<Pattern> {
            int left, right;

            protected override void Action(Pattern item) {
                for (int i = left; i <= right; i++)
                    item[i].Invert();
            }
            
            public FrameInvertedUndoEntry(Pattern pattern, int left, int right)
            : base($"Pattern Frames Inverted", pattern) {
                this.left = left;
                this.right = right;
            }
        }
        
        public class PlaybackModeUndoEntry: SimplePathUndoEntry<Pattern, PlaybackType> {
            protected override void Action(Pattern item, PlaybackType element) => item.Mode = element;
            
            public PlaybackModeUndoEntry(Pattern pattern, PlaybackType u, PlaybackType r)
            : base($"Pattern Playback Mode Changed to {r}", pattern, u, r) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Pattern, double> {
            protected override void Action(Pattern item, double element) => item.Gate = element;
            
            public GateUndoEntry(Pattern pattern, double u, double r)
            : base($"Pattern Gate Changed to {r}%", pattern, u / 100, r / 100) {}
        }
        
        public class RepeatsUndoEntry: SimplePathUndoEntry<Pattern, int> {
            protected override void Action(Pattern item, int element) => item.Repeats = element;
            
            public RepeatsUndoEntry(Pattern pattern, int u, int r)
            : base($"Pattern Repeats Changed to {r}", pattern, u, r) {}
        }
        
        public class PinchUndoEntry: SimplePathUndoEntry<Pattern, double> {
            protected override void Action(Pattern item, double element) => item.Pinch = element;
            
            public PinchUndoEntry(Pattern pattern, double u, double r)
            : base($"Pattern Pinch Changed to {r}", pattern, u, r) {}
        }
        
        public class BilateralUndoEntry: SimplePathUndoEntry<Pattern, bool> {
            protected override void Action(Pattern item, bool element) => item.Bilateral = element;
            
            public BilateralUndoEntry(Pattern pattern, bool u, bool r)
            : base($"Pattern Bilateral Changed to {(r? "Enabled" : "Disabled")}", pattern, u, r) {}
        }
        
        public class RootKeyUndoEntry: SimplePathUndoEntry<Pattern, int?> {
            protected override void Action(Pattern item, int? element) => item.RootKey = element;
            
            public RootKeyUndoEntry(Pattern pattern, int? u, int? r)
            : base($"Pattern Root Key Changed", pattern, u, r) {}
        }
        
        public class WrapUndoEntry: SimplePathUndoEntry<Pattern, bool> {
            protected override void Action(Pattern item, bool element) => item.Wrap = element;
            
            public WrapUndoEntry(Pattern pattern, bool u, bool r)
            : base($"Pattern Wrap Changed to {(r? "Enabled" : "Disabled")}", pattern, u, r) {}
        }
        
        public class ImportUndoEntry: SimplePathUndoEntry<Pattern, Pattern> {
            protected override void Action(Pattern item, Pattern element) {
                item.Repeats = element.Repeats;
                item.Gate = element.Gate;
                item.Pinch = element.Pinch;
                item.Bilateral = element.Bilateral;
                item.Frames = (from i in element.Frames select i.Clone()).ToList();
                item.Mode = element.Mode;
                item.Infinite = element.Infinite;
                item.RootKey = element.RootKey;
                item.Wrap = element.Wrap;
                item.Expanded = element.Expanded;
            }

            protected override void Dispose(Pattern item, Pattern undo, Pattern redo) {
                undo.Dispose();
                redo.Dispose();
            }
            
            public ImportUndoEntry(Pattern pattern, string filename, Pattern r)
            : base($"Pattern File Imported from {filename}", pattern, (Pattern)pattern.Clone(), r) {}
        }
    }
}