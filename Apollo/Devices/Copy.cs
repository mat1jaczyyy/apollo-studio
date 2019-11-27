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
            if (-180 <= angle && angle <= 180 && angle != Angles[index]) {
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
            public double X { get; private set; }
            public double Y { get; private set; }

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

            Type infoType = courier.Info.GetType();
            
            if ((CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) && infoType == typeof(PolyInfo)) {
                PolyInfo info = (PolyInfo)courier.Info;

                lock (info.locker) {
                    if (++info.index < info.offsets.Count && info.offsets[info.index] != -1) {
                        Signal m = info.n.Clone();
                        m.Index = (byte)info.offsets[info.index];
                        InvokeExit(m);

                        if (info.index == info.offsets.Count - 1)
                            poly.Remove(info);
                    }
                }

            } else if (CopyMode == CopyType.RandomLoop && infoType == typeof((Signal, List<int>))) {
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
                if (Offsets[i].Apply(n.Index, GridMode, Wrap, out int x, out int y, out int result))
                    validOffsets.Add(result);

                if (CopyMode == CopyType.Interpolate) {
                    int dx = x - px;
                    int dy = y - py;
                    
                    double angle = Angles[i] * Math.PI / 180;
                    
                    double magnitude = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
                    
                    DoubleTuple relMidPoint = new DoubleTuple(dx / 2.0, dy / 2.0);
                    
                    DoubleTuple cp1 = new DoubleTuple(
                        relMidPoint.X * Math.Cos(angle) + relMidPoint.Y * Math.Sin(angle) + px, 
                        (-relMidPoint.X * Math.Sin(angle) + relMidPoint.Y * Math.Cos(angle)) + py
                    );
                                              
                    DoubleTuple translatedMidPoint = new DoubleTuple(relMidPoint.X - dx, relMidPoint.Y - dy);
                    
                    DoubleTuple cp2 = new DoubleTuple(
                        px + dx + translatedMidPoint.X * Math.Cos(angle) - translatedMidPoint.Y * Math.Sin(angle), 
                        py + dy + translatedMidPoint.X * Math.Sin(angle) + translatedMidPoint.Y * Math.Cos(angle)
                    );
                                                      
                    DoubleTuple end = new DoubleTuple(x, y);
                    
                    int ax = Math.Abs(dx);
                    int ay = Math.Abs(dy);

                    int bx = (dx < 0)? -1 : 1;
                    int by = (dy < 0)? -1 : 1;
                    
                    int pointCount = (int)magnitude * 3;
                    
                    for (double pointIndex = 1; pointIndex <= pointCount; pointIndex++) {
                        IntTuple point = CubicBezierInterp(new DoubleTuple(px, py), cp1, cp2, end, pointIndex / pointCount).Round();
                        bool valid = Offset.Validate(point.X, point.Y, GridMode, Wrap, out int iresult);

                        if (iresult != interpolatedOffsets[interpolatedOffsets.Count - 1])
                            interpolatedOffsets.Add(valid? iresult : -1);
                    }
                }

                px = x;
                py = y;
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
    
        DoubleTuple CubicBezierInterp(DoubleTuple start, DoubleTuple cp1, DoubleTuple cp2, DoubleTuple end, double t){
            DoubleTuple A = Lerp(start, cp1, t);
            DoubleTuple B = Lerp(cp1, cp2, t);
            DoubleTuple C = Lerp(cp2, end, t);
            
            DoubleTuple D = Lerp(A, B, t);
            DoubleTuple E = Lerp(B, C, t);
            
            return Lerp(D, E, t);
        }
        
        DoubleTuple Lerp(DoubleTuple p1, DoubleTuple p2, double t) => p1 * (1.0 - t) + p2 * t;
    }
}