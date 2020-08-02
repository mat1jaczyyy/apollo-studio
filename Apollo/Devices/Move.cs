using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public override Device Clone() => new Move(Offset.Clone(), GridMode, Wrap) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Move(Offset offset = null, GridType gridmode = GridType.Full, bool wrap = false): base("move") {
            Offset = offset?? new Offset();
            GridMode = gridmode;
            Wrap = wrap;

            Offset.Changed += OffsetChanged;
        }

        public override void MIDIProcess(List<Signal> n)
            => InvokeExit(n.Where(i => i.Index == 100).Concat(n.SelectMany(i => {
                if (Offset.Apply(i.Index, GridMode, Wrap, out int x, out int y, out int result)) {
                    i.Index = (byte)result;
                    return new [] {i};
                }

                return Enumerable.Empty<Signal>();
            })).ToList());

        public override void Dispose() {
            if (Disposed) return;

            Offset.Dispose();
            base.Dispose();
        }

        public abstract class OffsetUpdatedUndoEntry: PathUndoEntry<Move> {
            int ux, uy, rx, ry;

            protected abstract void Action(Offset item, int u, int r);

            protected override void UndoPath(params Move[] item)
                => Action(item[0].Offset, ux, uy);

            protected override void RedoPath(params Move[] item)
                => Action(item[0].Offset, rx, ry);
            
            public OffsetUpdatedUndoEntry(string kind, Move move, int ux, int uy, int rx, int ry)
            : base($"Move Offset {kind} Changed to {rx},{ry}", move) {
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }

            protected OffsetUpdatedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                this.ux = reader.ReadInt32();
                this.uy = reader.ReadInt32();
                this.rx = reader.ReadInt32();
                this.ry = reader.ReadInt32();
            }

            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);

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
            
            public OffsetRelativeUndoEntry(Move move, int ux, int uy, int rx, int ry)
            : base("Relative", move, ux, uy, rx, ry) {}
            
            OffsetRelativeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class OffsetAbsoluteUndoEntry: OffsetUpdatedUndoEntry {
            protected override void Action(Offset item, int x, int y) {
                item.AbsoluteX = x;
                item.AbsoluteY = y;
            }
            
            public OffsetAbsoluteUndoEntry(Move move, int ux, int uy, int rx, int ry)
            : base("Absolute", move, ux, uy, rx, ry) {}
            
            OffsetAbsoluteUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public class OffsetSwitchedUndoEntry: SimplePathUndoEntry<Move, bool> {
            protected override void Action(Move item, bool element) => item.Offset.IsAbsolute = element;
            
            public OffsetSwitchedUndoEntry(Move move, bool u, bool r)
            : base($"Move Offset Switched to {(r? "Absolute" : "Relative")}", move, u, r) {}
            
            OffsetSwitchedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GridModeUndoEntry: EnumSimplePathUndoEntry<Move, GridType> {
            protected override void Action(Move item, GridType element) => item.GridMode = element;
            
            public GridModeUndoEntry(Move move, GridType u, GridType r, IEnumerable source)
            : base("Move Grid", move, u, r, source) {}
            
            GridModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class WrapUndoEntry: SimplePathUndoEntry<Move, bool> {
            protected override void Action(Move item, bool element) => item.Wrap = element;
            
            public WrapUndoEntry(Move move, bool u, bool r)
            : base($"Move Wrap Changed to {(r? "Enabled" : "Disabled")}", move, u, r) {}
            
            WrapUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}