using System;

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

        public override Device Clone() => new Layer(Target, BlendingMode) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Layer(int target = 0, BlendingType blending = BlendingType.Normal): base("layer") {
            Target = target;
            BlendingMode = blending;
        }

        public override void MIDIProcess(Signal n) {
            n.Layer = Target;
            n.BlendingMode = BlendingMode;

            MIDIExit?.Invoke(n);
        }
    }
}