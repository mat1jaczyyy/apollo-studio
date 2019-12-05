using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Copy: Device {
        Random RNG = new Random();
        
        public List<Offset> Offsets;
        List<int> Angles;

        public void Insert(int index, Offset offset = null, int angle = 0) {
            Offsets.Insert(index, offset?? new Offset());
            Offsets.Last().Changed += OffsetChanged;

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
            public List<Courier> timers = new List<Courier>();

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

        ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        ConcurrentDictionary<Signal, Courier> timers = new ConcurrentDictionary<Signal, Courier>();
        ConcurrentHashSet<PolyInfo> poly = new ConcurrentHashSet<PolyInfo>();

        public override Device Clone() => new Copy(_time.Clone(), _gate, CopyMode, GridMode, Wrap, (from i in Offsets select i.Clone()).ToList(), Angles.ToList()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Copy(Time time = null, double gate = 1, CopyType copymode = CopyType.Static, GridType gridmode = GridType.Full, bool wrap = false, List<Offset> offsets = null, List<int> angles = null): base("copy") {
            Time = time?? new Time(free: 500);
            Gate = gate;
            CopyMode = copymode;
            GridMode = gridmode;
            Wrap = wrap;
            Offsets = offsets?? new List<Offset>();
            Angles = angles?? new List<int>();

            foreach (Offset offset in Offsets)
                offset.Changed += OffsetChanged;
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

        void FireCourier((Signal n, List<int>) info, double time) {
            Courier courier = timers[info.n] = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = time
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            if ((CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) && courier.Info is PolyInfo info) {
                lock (info.locker) {
                    if (++info.index < info.offsets.Count && info.offsets[info.index] != -1) {
                        Signal m = info.n.Clone();
                        m.Index = (byte)info.offsets[info.index];
                        InvokeExit(m);

                        if (info.index == info.offsets.Count - 1)
                            poly.Remove(info);
                    }
                }

            } else if (CopyMode == CopyType.RandomLoop && courier.Info is Tuple<Signal, List<int>>) {
                (Signal n, List<int> offsets) = ((Signal, List<int>))courier.Info;
                HandleRandomLoop(n, offsets);
            }
        }

        void HandleRandomLoop(Signal original, List<int> offsets) {
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
                    InvokeExit(o);

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
                    InvokeExit(m);
                    FireCourier((original, offsets), _time * _gate);
                } else {
                    timers[n].Dispose();
                    timers.Remove(n, out Courier _);
                }
            }
        }

        public override void MIDIProcess(Signal n) {
            if (n.Index == 100) {
                InvokeExit(n);
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
                InvokeExit(n.Clone());

                for (int i = 1; i < validOffsets.Count; i++) {
                    Signal m = n.Clone();
                    m.Index = (byte)validOffsets[i];

                    InvokeExit(m);
                }

            } else if (CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) {
                if (!locker.ContainsKey(n)) locker[n] = new object();
                
                lock (locker[n]) {
                    InvokeExit(n.Clone());
                    
                    PolyInfo info = new PolyInfo(n, validOffsets);
                    poly.Add(info);

                    for (int i = 1; i < validOffsets.Count; i++)
                        FireCourier(info, _time * _gate * i);
                }

            } else if (CopyMode == CopyType.RandomSingle) {
                Signal m = n.Clone();
                n.Color = new Color();

                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = m.Index = (byte)validOffsets[RNG.Next(validOffsets.Count)];

                } else {
                    m.Index = (byte)buffer[n];
                    if (!m.Color.Lit) buffer.Remove(n, out int _);
                }

                InvokeExit(m);

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
    }
}