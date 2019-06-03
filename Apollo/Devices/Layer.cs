using System;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Layer: Device {
        public static readonly new string DeviceIdentifier = "layer";

        private int _target;
        public int Target {
            get => _target;
            set {
                if (_target != value) {
                    _target = value;
                    
                    if (Viewer?.SpecificViewer != null) ((LayerViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        Signal.BlendingType _mode;
        public string Mode {
            get => _mode.ToString();
            set {
                _mode = Enum.Parse<Signal.BlendingType>(value);

                if (Viewer?.SpecificViewer != null) ((LayerViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }

        public Signal.BlendingType GetBlendingMode() => _mode;

        public override Device Clone() => new Layer(Target, _mode) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Layer(int target = 0, Signal.BlendingType blending = Signal.BlendingType.Normal): base(DeviceIdentifier) {
            Target = target;
            _mode = blending;
        }

        public override void MIDIProcess(Signal n) {
            n.Layer = Target;
            n.BlendingMode = _mode;

            MIDIExit?.Invoke(n);
        }
    }
}