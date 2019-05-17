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

        public override Device Clone() => new Layer(Target);

        public Layer(int target = 0): base(DeviceIdentifier) => Target = target;

        public override void MIDIEnter(Signal n) {
            n.Layer = Target;

            MIDIExit?.Invoke(n);
        }
    }
}