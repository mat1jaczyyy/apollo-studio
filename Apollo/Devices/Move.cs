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
            
            protected override void UndoPath(params Move[] items){
                items[0].Offset.X = ux;
                items[0].Offset.Y = uy;
            }
            
            protected override void RedoPath(params Move[] items){
                items[0].Offset.X = rx;
                items[0].Offset.Y = ry;
            }
            
            public OffsetUndoEntry(Move Move, int ux, int uy, int rx, int ry)
            : base($"Move Offset Relative Changed to {rx},{ry}", Move){
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }
        }
        
        public class OffsetAbsoluteUndoEntry: PathUndoEntry<Move> {
            int ux, uy, rx, ry;
            
            protected override void UndoPath(params Move[] items){
                items[0].Offset.AbsoluteX = ux;
                items[0].Offset.AbsoluteY = uy;
            }
            
            protected override void RedoPath(params Move[] items){
                items[0].Offset.AbsoluteX = rx;
                items[0].Offset.AbsoluteY = ry;
            }
            
            public OffsetAbsoluteUndoEntry(Move Move, int ux, int uy, int rx, int ry)
            : base($"Move Offset Absolute Changed to {rx},{ry}", Move){
                this.ux = ux;
                this.uy = uy;
                this.rx = rx;
                this.ry = ry;
            }
        }
        
        public class OffsetSwitchedUndoEntry: PathUndoEntry<Move> {
            bool u, r;
            
            protected override void UndoPath(params Move[] items) => items[0].Offset.IsAbsolute = u;
            
            protected override void RedoPath(params Move[] items) => items[0].Offset.IsAbsolute = r;
            
            public OffsetSwitchedUndoEntry(Move Move, bool u, bool r)
            : base($"Move Offset Switched to {(r? "Absolute" : "Relative")}", Move){
                this.u = u;
                this.r = r;
            }
        }
        
        public class GridModeUndoEntry: PathUndoEntry<Move> {
            GridType u, r;
            
            protected override void UndoPath(params Move[] items) => items[0].GridMode = u;
            
            protected override void RedoPath(params Move[] items) => items[0].GridMode = r;
            
            public GridModeUndoEntry(Move Move, GridType u, GridType r)
            : base($"Move Grid Changed to {r.ToString()}", Move){
                this.u = u;
                this.r = r;
            }
        }
        
        public class WrapUndoEntry: PathUndoEntry<Move> {
            bool u, r;
            
            protected override void UndoPath(params Move[] items) => items[0].Wrap = u;
            
            protected override void RedoPath(params Move[] items) => items[0].Wrap = r;
            
            public WrapUndoEntry(Move Move, bool u, bool r)
            : base($"Move Wrap Changed to {(r? "Enabled" : "Disabled")}", Move){
                this.u = u;
                this.r = r;
            }
        }
    }
}