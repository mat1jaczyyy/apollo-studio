using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Paint: Device {
        public static readonly new string DeviceIdentifier = "paint";

        public Color Color;

        public override Device Clone() => new Paint(Color.Clone());

        public Paint(Color color = null): base(DeviceIdentifier) => Color = color?? new Color(63);

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit) n.Color = Color.Clone();
            MIDIExit?.Invoke(n);
        }
    }
}