using System;

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
        
        public class TargetUndoEntry : PathUndoEntry<LayerFilter>{
            int u, r;
            
            protected override void UndoPath(params LayerFilter[] items) => items[0].Target = u;
            
            protected override void RedoPath(params LayerFilter[] items) => items[0].Target = r;
            
            public TargetUndoEntry(LayerFilter LayerFilter, string unit, int u, int r)
            : base($"Layer Target Changed to {r}{unit}", LayerFilter){
                this.u = u;
                this.r = r;
            }
        }
        
        public class RangeUndoEntry : PathUndoEntry<LayerFilter>{
            int u, r;
            
            protected override void UndoPath(params LayerFilter[] items) => items[0].Range = u;
            
            protected override void RedoPath(params LayerFilter[] items) => items[0].Range = r;
            
            public RangeUndoEntry(LayerFilter LayerFilter, string unit, int u, int r)
            : base($"Layer Filter Range Changed to {r}{unit}", LayerFilter){
                this.u = u;
                this.r = r;
            }
        }
    }
}