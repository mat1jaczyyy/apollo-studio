using System.Collections;
using System.IO;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Move: Device {
        GridType _gridmode;
        public GridType GridMode {
            get => _gridmode;
            set {
                _gridmode = value;

                if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetGridMode(GridMode);
            }
        }

        public Offset Offset;

        void OffsetChanged(Offset sender) {
            if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetOffset(Offset);
        }

        bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                if (Viewer?.SpecificViewer != null) ((MoveViewer)Viewer.SpecificViewer).SetWrap(Wrap);
            }
        }

        public override Device Clone() => new Move(Offset.Clone()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Move(Offset offset = null, GridType gridmode = GridType.Full, bool wrap = false): base("move") {
            Offset = offset?? new Offset();
            GridMode = gridmode;
            Wrap = wrap;

            Offset.Changed += OffsetChanged;
        }

        public override void MIDIProcess(Signal n) {
            if (n.Index == 100) {
                InvokeExit(n);
                return;
            }

            if (Offset.Apply(n.Index, GridMode, Wrap, out int x, out int y, out int result)) {
                n.Index = (byte)result;
                InvokeExit(n);
            }
        }

        public override void Dispose() {
            if (Disposed) return;

            Offset.Dispose();
            base.Dispose();
        }
        
        public class OffsetUndoEntry: PathUndoEntry<Move> {
            int ux, uy, rx, ry;
            
            protected override void UndoPath(params Move[] items) {
                items[0].Offset.X = ux;
                items[0].Offset.Y = uy;
            }
            
            protected override void RedoPath(params Move[] items) {
                items[0].Offset.X = rx;
                items[0].Offset.Y = ry;
            }
            
            public OffsetUndoEntry(Move move, int ux, int uy, int rx, int ry)
            : base($"Move Offset Relative Changed to {rx},{ry}", move) {
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }
            
            OffsetUndoEntry(BinaryReader reader, int version): base(reader, version){
                ux = reader.ReadInt32();
                uy = reader.ReadInt32();
                rx = reader.ReadInt32();
                ry = reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(ux);
                writer.Write(uy);
                writer.Write(rx);
                writer.Write(ry);
            }
        }
        
        public class OffsetAbsoluteUndoEntry: PathUndoEntry<Move> {
            int ux, uy, rx, ry;
            
            protected override void UndoPath(params Move[] items) {
                items[0].Offset.AbsoluteX = ux;
                items[0].Offset.AbsoluteY = uy;
            }
            
            protected override void RedoPath(params Move[] items) {
                items[0].Offset.AbsoluteX = rx;
                items[0].Offset.AbsoluteY = ry;
            }
            
            public OffsetAbsoluteUndoEntry(Move move, int ux, int uy, int rx, int ry)
            : base($"Move Offset Absolute Changed to {rx},{ry}", move) {
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }
            
            OffsetAbsoluteUndoEntry(BinaryReader reader, int version): base(reader, version){
                ux = reader.ReadInt32();
                uy = reader.ReadInt32();
                rx = reader.ReadInt32();
                ry = reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(ux);
                writer.Write(uy);
                writer.Write(rx);
                writer.Write(ry);
            }
        }

        public class OffsetSwitchedUndoEntry: SimplePathUndoEntry<Move, bool> {
            protected override void Action(Move item, bool element) => item.Offset.IsAbsolute = element;
            
            public OffsetSwitchedUndoEntry(Move move, bool u, bool r)
            : base($"Move Offset Switched to {(r? "Absolute" : "Relative")}", move, u, r) {}
        }
        
        public class GridModeUndoEntry: EnumSimplePathUndoEntry<Move, GridType> {
            protected override void Action(Move item, GridType element) => item.GridMode = element;
            
            public GridModeUndoEntry(Move move, GridType u, GridType r, IEnumerable source)
            : base("Move Grid", move, u, r, source) {}
        }
        
        public class WrapUndoEntry: SimplePathUndoEntry<Move, bool> {
            protected override void Action(Move item, bool element) => item.Wrap = element;
            
            public WrapUndoEntry(Move move, bool u, bool r)
            : base($"Move Wrap Changed to {(r? "Enabled" : "Disabled")}", move, u, r) {}
        }
    }
}