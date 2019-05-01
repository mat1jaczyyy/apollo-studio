using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Layer: Device {
        public static readonly new string DeviceIdentifier = "layer";

        public int Target;

        public override Device Clone() => new Layer(Target);

        public Layer(int target = 0): base(DeviceIdentifier) => Target = target;

        public override void MIDIEnter(Signal n) {
            n.Layer = Target;

            MIDIExit?.Invoke(n);
        }
    }
}