using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Paint: Device {
        private Color _high = new Color(63), _low = new Color(0);

        public override Device Clone() {
            return new Paint(_high, _low);
        }

        public Paint() {}

        public Paint(Color color) {
            _high = color.Clone();
            _low = color.Clone();
        }

        public Paint(Color high, Color low) {
            _high = high.Clone();
            _low = low.Clone();
        }
        
        public Paint(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Paint(Color color, Action<Signal> exit) {
            _high = color.Clone();
            _low = color.Clone();
            MIDIExit = exit;
        }

        public Paint(Color high, Color low, Action<Signal> exit) {
            _high = high.Clone();
            _low = low.Clone();
            MIDIExit = exit;
        }

        private byte Scale(byte value, byte high, byte low) {
            if (value == 0)
                return 0;
            
            return (byte)(((high - low) * value) / 63 + low);
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit) {
                n.Color.Red = Scale(n.Color.Red, _high.Red, _low.Red);
                n.Color.Green = Scale(n.Color.Green, _high.Green, _high.Green);
                n.Color.Blue = Scale(n.Color.Blue, _high.Blue, _high.Blue);
            }

            if (MIDIExit != null)
                MIDIExit(n);
        }
    }
}