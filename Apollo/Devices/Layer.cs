using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Layer: Device {
        int _target;
        public int Target {
            get => _target;
            set {
                if (_target != value) {
                    _target = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LayerViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        BlendingType _mode;
        public BlendingType BlendingMode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((LayerViewer)Viewer.SpecificViewer).SetMode(BlendingMode);
            }
        }

        int _range;
        public int Range {
            get => _range;
            set {
                if (_range != value) {
                    _range = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LayerViewer)Viewer.SpecificViewer).SetRange(Range);
                }
            }
        }

        public override Device Clone() => new Layer(Target, BlendingMode, Range) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Layer(int target = 0, BlendingType blending = BlendingType.Normal, int range = 200): base("layer") {
            Target = target;
            BlendingMode = blending;
            Range = range;
        }

        public override void MIDIProcess(Signal n) {
            n.Layer = Target;
            n.BlendingMode = BlendingMode;
            n.BlendingRange = Range;

            InvokeExit(n);
        }
        
        public class TargetUndoEntry : PathUndoEntry<Layer>{
            int u, r;
            
            protected override void UndoPath(params Layer[] items) => items[0].Target = u;
            
            protected override void RedoPath(params Layer[] items) => items[0].Target = r;
            
            public TargetUndoEntry(Layer Layer, string unit, int u, int r)
            : base($"Layer Target Changed to {r}{unit}", Layer){
                this.u = u;
                this.r = r;
            }
        }
        
        public class ModeUndoEntry : PathUndoEntry<Layer>{
            BlendingType u, r;
            
            protected override void UndoPath(params Layer[] items) => items[0].BlendingMode = u;
            
            protected override void RedoPath(params Layer[] items) => items[0].BlendingMode = r;
            
            public ModeUndoEntry(Layer Layer, BlendingType u, BlendingType r)
            : base($"Layer Blending Changed to {r}", Layer){
                this.u = u;
                this.r = r;
            }
        }
        
        public class RangeUndoEntry : PathUndoEntry<Layer>{
            int u, r;
            
            protected override void UndoPath(params Layer[] items) => items[0].Range = u;
            
            protected override void RedoPath(params Layer[] items) => items[0].Range = r;
            
            public RangeUndoEntry(Layer Layer, string unit, int u, int r)
            : base($"Layer Range Changed to {r}{unit}", Layer){
                this.u = u;
                this.r = r;
            }
        }
    }
}