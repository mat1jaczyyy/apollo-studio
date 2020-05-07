using System;
using System.IO;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class LayerFilter: Device {
        int _target;
        public int Target {
            get => _target;
            set {
                if (_target != value) {
                    _target = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LayerFilterViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        int _range;
        public int Range {
            get => _range;
            set {
                if (_range != value) {
                    _range = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LayerFilterViewer)Viewer.SpecificViewer).SetRange(Range);
                }
            }
        }

        public override Device Clone() => new LayerFilter(Target, Range) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public LayerFilter(int target = 0, int range = 0): base("layerfilter", "Layer Filter") {
            Target = target;
            Range = range;
        }

        public override void MIDIProcess(Signal n) {
            if (Math.Abs(n.Layer - Target) <= Range)
                InvokeExit(n);
        }
        
        public class TargetUndoEntry: SimplePathUndoEntry<LayerFilter, int> {
            protected override void Action(LayerFilter item, int element) => item.Target = element;
            
            public TargetUndoEntry(LayerFilter filter, int u, int r)
            : base($"Layer Filter Target Changed to {r}", filter, u, r) {}
            
            TargetUndoEntry(BinaryReader reader, int version): base(reader, version){}
        }
        
        public class RangeUndoEntry: SimplePathUndoEntry<LayerFilter, int> {
            protected override void Action(LayerFilter item, int element) => item.Range = element;
            
            public RangeUndoEntry(LayerFilter filter, int u, int r)
            : base($"Layer Filter Range Changed to {r}", filter, u, r) {}
            
            RangeUndoEntry(BinaryReader reader, int version): base(reader, version){}
        }
    }
}