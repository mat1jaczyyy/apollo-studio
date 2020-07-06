using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.Binary;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    //! Heaven incompatible
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

        public override void MIDIProcess(Signal n) {
            
        }

        protected override void Stop() {

        }

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            foreach (Offset offset in Offsets) offset.Dispose();
            Time.Dispose();
            base.Dispose();
        }
            
        public class RateUndoEntry: SimplePathUndoEntry<Copy, int> {
            protected override void Action(Copy item, int element) => item.Time.Free = element;
            
            public RateUndoEntry(Copy copy, int u, int r)
            : base($"Copy Rate Changed to {r}ms", copy, u, r) {}
            
            RateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }   
        
        public class RateModeUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Time.Mode = element;
            
            public RateModeUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Rate Switched to {(r? "Steps" : "Free")}", copy, u, r) {}
            
            RateModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class RateStepUndoEntry: SimplePathUndoEntry<Copy, int> {
            protected override void Action(Copy item, int element) => item.Time.Length.Step = element;
            
            public RateStepUndoEntry(Copy copy, int u, int r)
            : base($"Copy Rate Changed to {Length.Steps[r]}", copy, u, r) {}
            
            RateStepUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Copy, double> {
            protected override void Action(Copy item, double element) => item.Gate = element;
            
            public GateUndoEntry(Copy copy, double u, double r)
            : base($"Copy Gate Changed to {r}%", copy, u / 100, r / 100) {}
            
            GateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class CopyModeUndoEntry: EnumSimplePathUndoEntry<Copy, CopyType> {
            protected override void Action(Copy item, CopyType element) => item.CopyMode = element;
            
            public CopyModeUndoEntry(Copy copy, CopyType u, CopyType r, IEnumerable source)
            : base("Copy Mode", copy, u, r, source) {}
            
            CopyModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GridModeUndoEntry: EnumSimplePathUndoEntry<Copy, GridType> {
            protected override void Action(Copy item, GridType element) => item.GridMode = element;
            
            public GridModeUndoEntry(Copy copy, GridType u, GridType r, IEnumerable source)
            : base("Copy Grid", copy, u, r, source) {}
            
            GridModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class PinchUndoEntry: SimplePathUndoEntry<Copy, double> {
            protected override void Action(Copy item, double element) => item.Pinch = element;
            
            public PinchUndoEntry(Copy copy, double u, double r)
            : base($"Copy Pinch Changed to {r}", copy, u, r) {}
            
            PinchUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class BilateralUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Bilateral = element;
            
            public BilateralUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Bilateral Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
            
            BilateralUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ReverseUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Reverse = element;
            
            public ReverseUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Reverse Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
            
            ReverseUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class InfiniteUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Infinite = element;
            
            public InfiniteUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Infinite Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
            
            InfiniteUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class WrapUndoEntry: SimplePathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, bool element) => item.Wrap = element;
            
            public WrapUndoEntry(Copy copy, bool u, bool r)
            : base($"Copy Wrap Changed to {(r? "Enabled" : "Disabled")}", copy, u, r) {}
            
            WrapUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class OffsetInsertUndoEntry: PathUndoEntry<Copy> {
            int index;
            
            protected override void UndoPath(params Copy[] items) => items[0].Remove(index);
            protected override void RedoPath(params Copy[] items) => items[0].Insert(index);
            
            public OffsetInsertUndoEntry(Copy copy, int index)
            : base($"Copy Offset {index + 1} Inserted", copy) => this.index = index;
            
            OffsetInsertUndoEntry(BinaryReader reader, int version)
            : base(reader, version) => index = reader.ReadInt32();
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(index);
            }
        }
        
        public class OffsetRemoveUndoEntry: PathUndoEntry<Copy> {
            int index;
            Offset offset;
            int angle;
            
            protected override void UndoPath(params Copy[] items) => items[0].Insert(index, offset.Clone(), angle);
            protected override void RedoPath(params Copy[] items) => items[0].Remove(index);
            
            protected override void OnDispose() => offset.Dispose();
            
            public OffsetRemoveUndoEntry(Copy copy, Offset offset, int angle, int index)
            : base($"Copy Offset {index + 1} Removed", copy) {
                this.index = index;
                this.offset = offset.Clone();
                this.angle = angle;
            }
            
            OffsetRemoveUndoEntry(BinaryReader reader, int version): base(reader, version) {
                index = reader.ReadInt32();
                offset = Decoder.Decode<Offset>(reader, version);
                angle = reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(index);
                Encoder.Encode(writer, offset);
                writer.Write(angle);
            }
        }

        public abstract class OffsetUpdatedUndoEntry: PathUndoEntry<Copy> {
            int index, ux, uy, rx, ry;

            protected abstract void Action(Offset item, int u, int r);

            protected override void UndoPath(params Copy[] item)
                => Action(item[0].Offsets[index], ux, uy);

            protected override void RedoPath(params Copy[] item)
                => Action(item[0].Offsets[index], rx, ry);
            
            public OffsetUpdatedUndoEntry(string kind, Copy copy, int index, int ux, int uy, int rx, int ry)
            : base($"Copy Offset {index + 1} {kind} Changed to {rx},{ry}", copy) {
                this.index = index;
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }

            protected OffsetUpdatedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                this.index = reader.ReadInt32();
                this.ux = reader.ReadInt32();
                this.uy = reader.ReadInt32();
                this.rx = reader.ReadInt32();
                this.ry = reader.ReadInt32();
            }

            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);

                writer.Write(index);
                writer.Write(ux);
                writer.Write(uy);
                writer.Write(rx);
                writer.Write(ry);
            }
        }

        public class OffsetRelativeUndoEntry: OffsetUpdatedUndoEntry {
            protected override void Action(Offset item, int x, int y) {
                item.X = x;
                item.Y = y;
            }
            
            public OffsetRelativeUndoEntry(Copy copy, int index, int ux, int uy, int rx, int ry)
            : base("Relative", copy, index, ux, uy, rx, ry) {}

            OffsetRelativeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public class OffsetAbsoluteUndoEntry: OffsetUpdatedUndoEntry {
            protected override void Action(Offset item, int x, int y) {
                item.AbsoluteX = x;
                item.AbsoluteY = y;
            }
            
            public OffsetAbsoluteUndoEntry(Copy copy, int index, int ux, int uy, int rx, int ry)
            : base("Absolute", copy, index, ux, uy, rx, ry) {}

            OffsetAbsoluteUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public class OffsetSwitchedUndoEntry: SimpleIndexPathUndoEntry<Copy, bool> {
            protected override void Action(Copy item, int index, bool element) => item.Offsets[index].IsAbsolute = element;
            
            public OffsetSwitchedUndoEntry(Copy copy, int index, bool u, bool r)
            : base($"Copy Offset {index + 1} Switched to {(r? "Absolute" : "Relative")}", copy, index, u, r) {}
            
            OffsetSwitchedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public class OffsetAngleUndoEntry: SimpleIndexPathUndoEntry<Copy, int> {
            protected override void Action(Copy item, int index, int element) => item.SetAngle(index, element);
            
            public OffsetAngleUndoEntry(Copy copy, int index, int u, int r)
            : base($"Copy Angle {index + 1} Changed to {r}°", copy, index, u, r) {}
            
            OffsetAngleUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}