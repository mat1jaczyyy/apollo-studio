using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

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
    }
}