using System.Collections.Generic;
using System.IO;

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

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Target, BlendingMode, Range };

        public Layer(int target = 0, BlendingType blending = BlendingType.Normal, int range = 200): base("layer") {
            Target = target;
            BlendingMode = blending;
            Range = range;
        }

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(i => {
                i.Layer = Target;
                i.BlendingMode = BlendingMode;
                i.BlendingRange = Range;
            });

            InvokeExit(n);
        }
        
        public class TargetUndoEntry: SimplePathUndoEntry<Layer, int> {
            protected override void Action(Layer item, int element) => item.Target = element;
            
            public TargetUndoEntry(Layer layer, int u, int r)
            : base($"Layer Target Changed to {r}", layer, u, r) {}
            
            TargetUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ModeUndoEntry: SimplePathUndoEntry<Layer, BlendingType> {
            protected override void Action(Layer item, BlendingType element) => item.BlendingMode = element;
            
            public ModeUndoEntry(Layer layer, BlendingType u, BlendingType r)
            : base($"Layer Blending Changed to {r}", layer, u, r) {}
            
            ModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class RangeUndoEntry: SimplePathUndoEntry<Layer, int> {
            protected override void Action(Layer item, int element) => item.Range = element;
            
            public RangeUndoEntry(Layer layer, int u, int r)
            : base($"Layer Range Changed to {r}", layer, u, r) {}
            
            RangeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}