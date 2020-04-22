using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Copy: Device {
        Random RNG = new Random();
        
        public List<Offset> Offsets;
        List<int> Angles;

        public void Insert(int index, Offset offset = null, int angle = 0) {
            Offsets.Insert(index, offset?? new Offset());
            Offsets[index].Changed += OffsetChanged;

            Angles.Insert(index, angle);

            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).Contents_Insert(index, Offsets[index], Angles[index]);
        }

        public void Remove(int index) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).Contents_Remove(index);

            Offsets[index].Changed -= OffsetChanged;
            Offsets.RemoveAt(index);

            Angles.RemoveAt(index);
        }

        void OffsetChanged(Offset sender) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetOffset(Offsets.IndexOf(sender), sender);
        }

        public int GetAngle(int index) => Angles[index];
        public void SetAngle(int index, int angle) {
            if (-150 <= angle && angle <= 150 && angle != Angles[index]) {
                Angles[index] = angle;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetOffsetAngle(index, angle);
            }
        }

        Time _time;
        public Time Time {
            get => _time;
            set {
                if (_time != null) {
                    _time.FreeChanged -= FreeChanged;
                    _time.ModeChanged -= ModeChanged;
                    _time.StepChanged -= StepChanged;
                }

                _time = value;

                if (_time != null) {
                    _time.Minimum = 10;
                    _time.Maximum = 5000;

                    _time.FreeChanged += FreeChanged;
                    _time.ModeChanged += ModeChanged;
                    _time.StepChanged += StepChanged;
                }
            }
        }

        void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetRateValue(value);
        }

        void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetRateStep(value);
        }

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetWrap(Wrap);
            }
        }

        class PolyInfo {
            public Signal n;
            public int index = 0;
            public object locker = new object();
            public List<int> offsets;
            public List<Courier<PolyInfo>> timers = new List<Courier<PolyInfo>>();

            public PolyInfo(Signal init_n, List<int> init_offsets) {
                n = init_n;
                offsets = init_offsets;
            }
        }

        class DoubleTuple {
            public double X;
            public double Y;

            public DoubleTuple(double x = 0, double y = 0) {
                X = x;
                Y = y;
            }

            public IntTuple Round() => new IntTuple((int)Math.Round(X), (int)Math.Round(Y));

            public static DoubleTuple operator *(DoubleTuple t, double f) => new DoubleTuple(t.X * f, t.Y * f);
            public static DoubleTuple operator +(DoubleTuple a, DoubleTuple b) => new DoubleTuple(a.X + b.X, a.Y + b.Y);
        };

        class IntTuple {
            public int X;
            public int Y;

            public IntTuple(int x, int y) {
                X = x;
                Y = y;
            }

            public IntTuple Apply(Func<int, int> action) => new IntTuple(
                action.Invoke(X),
                action.Invoke(Y)
            );
        }

        CopyType _copymode;
        public CopyType CopyMode {
            get => _copymode;
            set {
                _copymode = value;
                
                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetCopyMode(CopyMode);

                Stop();
            }
        }

        GridType _gridmode;
        public GridType GridMode {
            get => _gridmode;
            set {
                _gridmode = value;
                
                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetGridMode(GridMode);
            }
        }
        
        double _pinch;
        public double Pinch {
            get => _pinch;
            set {
                if (-2 <= value && value <= 2) {
                    _pinch = value;
                    
                    if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer)?.SetPinch(Pinch);
                }
            }
        }
        
        bool _bilateral;
        public bool Bilateral {
            get => _bilateral;
            set {
                if (value != _bilateral) {
                    _bilateral = value;
                    
                    if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer)?.SetBilateral(Bilateral);
                }
            }
        }

        bool _reverse;
        public bool Reverse {
            get => _reverse;
            set {
                _reverse = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetReverse(Reverse);
            }
        }
        
        bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                _infinite = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetInfinite(Infinite);
            }
        }

        ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        ConcurrentDictionary<Signal, Courier<ValueTuple<Signal, List<int>>>> timers = new ConcurrentDictionary<Signal, Courier<ValueTuple<Signal, List<int>>>>();
        ConcurrentHashSet<PolyInfo> poly = new ConcurrentHashSet<PolyInfo>();

        ConcurrentDictionary<Signal, HashSet<Signal>> screen = new ConcurrentDictionary<Signal, HashSet<Signal>>();
        ConcurrentDictionary<Signal, object> screenlocker = new ConcurrentDictionary<Signal, object>();

        public override Device Clone() => new Copy(_time.Clone(), _gate, Pinch, Bilateral, Reverse, Infinite, CopyMode, GridMode, Wrap, Offsets.Select(i => i.Clone()).ToList(), Angles.ToList()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Copy(Time time = null, double gate = 1, double pinch = 0, bool bilateral = false, bool reverse = false, bool infinite = false, CopyType copymode = CopyType.Static, GridType gridmode = GridType.Full, bool wrap = false, List<Offset> offsets = null, List<int> angles = null): base("copy") {
            Time = time?? new Time(free: 500);
            Gate = gate;
            Pinch = pinch;
            Bilateral = bilateral;

            Reverse = reverse;
            Infinite = infinite;

            CopyMode = copymode;
            GridMode = gridmode;
            Wrap = wrap;

            Offsets = offsets?? new List<Offset>();
            Angles = angles?? new List<int>();

            foreach (Offset offset in Offsets)
                offset.Changed += OffsetChanged;
        }

        void ScreenOutput(Signal o, Signal i) {
            bool on = i.Color.Lit;
            i.Color = new Color();
            Signal k = o.With(o.Index, new Color());

            if (!screenlocker.ContainsKey(k))
                screenlocker[k] = new object();
            
            lock (screenlocker[k]) {
                if (!screen.ContainsKey(k))
                    screen[k] = new HashSet<Signal>();

                if (on) {
                    if (!screen[k].Contains(i))
                        screen[k].Add(i);

                    InvokeExit(o.Clone());

                } else {
                    if (screen[k].Contains(i))
                        screen[k].Remove(i);
                    
                    if (screen[k].Count == 0)
                        InvokeExit(o.Clone());
                }
            }
        }

        void FireCourier(PolyInfo info, double time)
            => info.timers.Add(new Courier<PolyInfo>(time, info, Tick));

        void FireCourier((Signal n, List<int>) info, double time)
            => timers[info.n.With(info.n.Index, new Color())] = new Courier<ValueTuple<Signal, List<int>>>(time, info, Tick);

        void Tick(Courier<PolyInfo> sender, PolyInfo info) {
            if (Disposed) return;
            
            if (CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) {
                lock (info.locker) {
                    if (++info.index < info.offsets.Count && info.offsets[info.index] != -1) {
                        Signal m = info.n.Clone();
                        m.Index = (byte)info.offsets[info.index];
                        ScreenOutput(m, info.n.Clone());

                        if (info.index == info.offsets.Count - 1)
                            poly.Remove(info);
                    }
                }
            }
        }

        void Tick(Courier<(Signal n, List<int> offsets)> sender, (Signal n, List<int> offsets) info) {
            if (Disposed) return;
            
            if (CopyMode == CopyType.RandomLoop)
                HandleRandomLoop(info.n, info.offsets);
        }

        void HandleRandomLoop(Signal original, List<int> offsets) {
            Signal n = original.With(original.Index, new Color());
            Signal m = original.Clone();

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
                    ScreenOutput(o, original.Clone());

                    if (m.Color.Lit) {
                        if (offsets.Count > 1) {
                            int old = buffer[n];
                            buffer[n] = RNG.Next(offsets.Count - 1);
                            if (buffer[n] >= old) buffer[n]++;
                        }
                        m.Index = (byte)offsets[buffer[n]];
                    
                    } else buffer.Remove(n, out _);
                }

                if (buffer.ContainsKey(n)) {
                    ScreenOutput(m, original.Clone());
                    FireCourier((original, offsets), _time * _gate);
                } else {
                    timers[n].Dispose();
                    timers.Remove(n, out _);
                }
            }
        }

        public override void MIDIProcess(Signal n) {
            if (n.Index == 100) {
                ScreenOutput(n.Clone(), n.Clone());
                return;
            }

            int px = n.Index % 10;
            int py = n.Index / 10;

            List<int> validOffsets = new List<int>() {n.Index};
            List<int> interpolatedOffsets = new List<int>() {n.Index};

            for (int i = 0; i < Offsets.Count; i++) {
                if (Offsets[i].Apply(n.Index, GridMode, Wrap, out int _x, out int _y, out int result))
                    validOffsets.Add(result);

                if (CopyMode == CopyType.Interpolate) {
                    double angle = Angles[i] / 90.0 * Math.PI;
                    
                    double x = _x;
                    double y = _y;
                    
                    int pointCount;
                    Func<int, DoubleTuple> pointGenerator;

                    DoubleTuple source = new DoubleTuple(px, py);
                    DoubleTuple target = new DoubleTuple(_x, _y);

                    if (angle != 0) {
                        // https://www.desmos.com/calculator/hizsxmojxz

                        double diam = Math.Sqrt(Math.Pow(px - x, 2) + Math.Pow(py - y, 2));
                        double commonTan = Math.Atan((px - x) / (y - py));

                        double cord = diam / (2 * Math.Tan(Math.PI - angle / 2)) * (((y - py) >= 0)? 1 : -1);
                        
                        DoubleTuple center = new DoubleTuple(
                            (px + x) / 2 + Math.Cos(commonTan) * cord,
                            (py + y) / 2 + Math.Sin(commonTan) * cord
                        );
                        
                        double radius = diam / (2 * Math.Sin(Math.PI - angle / 2));
                        
                        double u = (Convert.ToInt32(angle < 0) * (Math.PI + angle) + Math.Atan2(py - center.Y, px - center.X)) % (2 * Math.PI);
                        double v = (Convert.ToInt32(angle < 0) * (Math.PI - angle) + Math.Atan2(y - center.Y, x - center.X)) % (2 * Math.PI);
                        v += (u <= v)? 0 : 2 * Math.PI;
                        
                        double startAngle = (angle < 0)? v : u;
                        double endAngle = (angle < 0)? u : v;

                        pointCount = (int)(Math.Abs(radius) * Math.Abs(endAngle - startAngle) * 1.5);
                        pointGenerator = t => CircularInterp(center, radius, startAngle, endAngle, t, pointCount);
                        
                    } else {
                        IntTuple p = new IntTuple(px, py);

                        IntTuple d = new IntTuple(
                            _x - px,
                            _y - py
                        );

                        IntTuple a = d.Apply(v => Math.Abs(v));
                        IntTuple b = d.Apply(v => (v < 0)? -1 : 1);

                        pointCount = Math.Max(a.X, a.Y);
                        pointGenerator = t => LineGenerator(p, a, b, t);
                    }
                        
                    for (int p = 1; p <= pointCount; p++) {
                        DoubleTuple doublepoint = pointGenerator.Invoke(p);
                        IntTuple point = doublepoint.Round();

                        if (Math.Pow(doublepoint.X - point.X, 2.16) + Math.Pow(doublepoint.Y - point.Y, 2.16) > .25) continue;

                        bool valid = Offset.Validate(point.X, point.Y, GridMode, Wrap, out int iresult);

                        if (iresult != interpolatedOffsets.Last())
                            interpolatedOffsets.Add(valid? iresult : -1);
                    }
                }

                px = _x;
                py = _y;
            }

            if (CopyMode == CopyType.Interpolate) validOffsets = interpolatedOffsets;

            if (CopyMode == CopyType.Static) {
                ScreenOutput(n.Clone(), n.Clone());

                for (int i = 1; i < validOffsets.Count; i++) {
                    Signal m = n.Clone();
                    m.Index = (byte)validOffsets[i];

                    ScreenOutput(m, n.Clone());
                }

            } else if (CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) {
                if (!locker.ContainsKey(n)) locker[n] = new object();
                
                lock (locker[n]) {
                    if (Reverse) validOffsets.Reverse();

                    if (validOffsets[0] != -1)
                        ScreenOutput(n.With((byte)validOffsets[0], n.Color), n.Clone());
                    
                    PolyInfo info = new PolyInfo(n.Clone(), validOffsets);
                    poly.Add(info);

                    double total = _time * _gate * (validOffsets.Count - 1);

                    for (int i = 1; i < validOffsets.Count; i++)
                        if (!Infinite || i < validOffsets.Count - 1 || n.Color.Lit)
                            FireCourier(info, Pincher.ApplyPinch(_time * _gate * i, total, Pinch, Bilateral));
                }

            } else if (CopyMode == CopyType.RandomSingle) {
                Signal m = n.Clone();
                n.Color = new Color();

                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = m.Index = (byte)validOffsets[RNG.Next(validOffsets.Count)];

                } else {
                    m.Index = (byte)buffer[n];
                    if (!m.Color.Lit) buffer.Remove(n, out _);
                }

                ScreenOutput(m, n.Clone());

            } else if (CopyMode == CopyType.RandomLoop) HandleRandomLoop(n, validOffsets);
        }

        protected override void Stop() {
            foreach (Courier i in timers.Values) i.Dispose();
            timers.Clear();

            foreach (PolyInfo info in poly) {
                foreach (Courier i in info.timers) i.Dispose();
                info.timers.Clear();
            }
            poly.Clear();

            buffer.Clear();
            locker.Clear();

            screen.Clear();
            screenlocker.Clear();
        }

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            foreach (Offset offset in Offsets) offset.Dispose();
            Time.Dispose();
            base.Dispose();
        }
    
        DoubleTuple CircularInterp(DoubleTuple center, double radius, double startAngle, double endAngle, double t, double pointCount) {
            double angle = startAngle + (endAngle - startAngle) * ((double)t / pointCount);

            return new DoubleTuple(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle)
            );
        }
        
        DoubleTuple LineGenerator(IntTuple p, IntTuple a, IntTuple b, int t) => (a.X > a.Y)
            ? new DoubleTuple(
                p.X + t * b.X,
                p.Y + (int)Math.Round((double)t / a.X * a.Y) * b.Y
            )
            : new DoubleTuple(
                p.X + (int)Math.Round((double)t / a.Y * a.X) * b.X,
                p.Y + t * b.Y
            );
            
        public class RateUndoEntry: SimplePathUndoEntry<Copy, int> {
            protected override void Action(Copy item, int element) => item.Time.Free = element;
            
            public RateUndoEntry(Copy copy, int u, int r)
            : base($"Copy Rate Changed to {r}ms", copy, u, r) {}
        }   
        
        public class RateModeUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Time.Mode = element;
            
            public RateModeUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Rate Switched to {(r? "Steps" : "Free")}", copy, u, r) {}
        }
        
        public class RateStepUndoEntry: SimplePathUndoEntry<Copy, int> {
            protected override void Action(Copy item, int element) => item.Time.Length.Step = element;
            
            public RateStepUndoEntry(Copy copy, int u, int r)
            : base($"Copy Rate Changed to {Length.Steps[r]}", copy, u, r) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Copy, double> {
            protected override void Action(Copy item, double element) => item.Gate = element;
            
            public GateUndoEntry(Copy copy, double u, double r)
            : base($"Copy Gate Changed to {r}%", copy, u / 100, r / 100) {}
        }
        
        public class CopyModeUndoEntry: EnumSimplePathUndoEntry<Copy, CopyType> {
            protected override void Action(Copy item, CopyType element) => item.CopyMode = element;
            
            public CopyModeUndoEntry(Copy copy, CopyType u, CopyType r, IEnumerable source)
            : base("Copy Mode", copy, u, r, source) {}
        }
        
        public class GridModeUndoEntry: EnumSimplePathUndoEntry<Copy, GridType> {
            protected override void Action(Copy item, GridType element) => item.GridMode = element;
            
            public GridModeUndoEntry(Copy copy, GridType u, GridType r, IEnumerable source)
            : base("Copy Grid", copy, u, r, source) {}
        }
        
        public class PinchUndoEntry: SimplePathUndoEntry<Copy, double> {
            protected override void Action(Copy item, double element) => item.Pinch = element;
            
            public PinchUndoEntry(Copy copy, double u, double r)
            : base($"Copy Pinch Changed to {r}", copy, u, r) {}
        }
        
        public class BilateralUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Bilateral = element;
            
            public BilateralUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Bilateral Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
        }
        
        public class ReverseUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Reverse = element;
            
            public ReverseUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Reverse Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
        }
        
        public class InfiniteUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Infinite = element;
            
            public InfiniteUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Infinite Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
        }
        
        public class WrapUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Wrap = element;
            
            public WrapUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Wrap Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
        }
        
        public class OffsetInsertUndoEntry: PathUndoEntry<Copy> {
            int index;
            
            protected override void UndoPath(params Copy[] items) => items[0].Remove(index);
            protected override void RedoPath(params Copy[] items) => items[0].Insert(index);
            
            public OffsetInsertUndoEntry(Copy copy, int index)
            : base($"Copy Offset {index + 1} Inserted", copy) => this.index = index;
        }
        
        public class OffsetRemoveUndoEntry: PathUndoEntry<Copy> {
            int index;
            Offset offset;
            
            protected override void UndoPath(params Copy[] items) => items[0].Insert(index, offset.Clone());
            protected override void RedoPath(params Copy[] items) => items[0].Remove(index);
            
            protected override void OnDispose() => offset.Dispose();
            
            public OffsetRemoveUndoEntry(Copy copy, Offset offset, int index)
            : base($"Copy Offset {index + 1} Removed", copy) {
                this.index = index;
                this.offset = offset.Clone();
            }
        }

        public class OffsetRelativeUndoEntry: PathUndoEntry<Copy> {
            int index, ux, uy, rx, ry;

            protected override void UndoPath(params Copy[] item) {
                item[0].Offsets[index].X = ux;
                item[0].Offsets[index].Y = uy;
            }

            protected override void RedoPath(params Copy[] item) {
                item[0].Offsets[index].X = rx;
                item[0].Offsets[index].Y = ry;
            }
            
            public OffsetRelativeUndoEntry(Copy copy, int index, int ux, int uy, int rx, int ry)
            : base($"Copy Offset {index + 1} Relative Changed to {rx},{ry}", copy) {
                this.index = index;
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }
        }

        public class OffsetAbsoluteUndoEntry: PathUndoEntry<Copy> {
            int index, ux, uy, rx, ry;

            protected override void UndoPath(params Copy[] item) {
                item[0].Offsets[index].AbsoluteX = ux;
                item[0].Offsets[index].AbsoluteY = uy;
            }

            protected override void RedoPath(params Copy[] item) {
                item[0].Offsets[index].AbsoluteX = rx;
                item[0].Offsets[index].AbsoluteY = ry;
            }
            
            public OffsetAbsoluteUndoEntry(Copy copy, int index, int ux, int uy, int rx, int ry)
            : base($"Copy Offset {index + 1} Absolute Changed to {rx},{ry}", copy) {
                this.index = index;
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }
        }

        public class OffsetSwitchedUndoEntry: SimpleIndexPathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, int index, bool element) => item.Offsets[index].IsAbsolute = element;
            
            public OffsetSwitchedUndoEntry(Copy copy, int index, bool u, bool r)
            : base($"Copy Offset {index + 1} Switched to {(r? "Absolute" : "Relative")}", copy, index, u, r) {}
        }

        public class OffsetAngleUndoEntry: SimpleIndexPathUndoEntry<Copy, int> {
            protected override void Action(Copy item, int index, int element) => item.SetAngle(index, element);
            
            public OffsetAngleUndoEntry(Copy copy, int index, int u, int r)
            : base($"Copy Angle {index + 1} Changed to {r}°", copy, index, u, r) {}
        }
    }
}