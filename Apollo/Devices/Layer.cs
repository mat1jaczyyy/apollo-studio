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
        
        public class TargetUndoEntry: SimpleUndoEntry<Layer, int> {
            protected override void Action(Layer item, int element) => item.Target = element;
            
            public TargetUndoEntry(Layer layer, int u, int r)
            : base($"Layer Target Changed to {r}", layer, u, r) {}
        }
        
        public class ModeUndoEntry: SimpleUndoEntry<Layer, BlendingType> {
            protected override void Action(Layer item, BlendingType element) => item.BlendingMode = element;
            
            public ModeUndoEntry(Layer layer, BlendingType u, BlendingType r)
            : base($"Layer Blending Changed to {r}", layer, u, r) {}
        }
        
        public class RangeUndoEntry: SimpleUndoEntry<Layer, int> {
            protected override void Action(Layer item, int element) => item.Range = element;
            
            public RangeUndoEntry(Layer layer, int u, int r)
            : base($"Layer Range Changed to {r}", layer, u, r) {}
        }
    }
}