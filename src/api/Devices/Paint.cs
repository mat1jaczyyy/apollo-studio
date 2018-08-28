using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Paint: Device {
        private Color _high, _low;

        public override Device Clone() {
            return new Paint(_high, _low);
        }

        public Paint() {
            _high = new Color(63);
            _low = new Color(1);
            MIDIExit = null;
        }

        public Paint(Color color) {
            _high = color.Clone();
            _low = color.Clone();
            MIDIExit = null;
        }

        public Paint(Color high, Color low) {
            _high = high;
            _low = low;
            MIDIExit = null;
        }
        
        public Paint(Action<Signal> exit) {
            _high = new Color(63);
            _low = new Color(1);
            MIDIExit = exit;
        }

        public Paint(Color color, Action<Signal> exit) {
            _high = color.Clone();
            _low = color.Clone();
            MIDIExit = exit;
        }

        public Paint(Color high, Color low, Action<Signal> exit) {
            _high = high;
            _low = low;
            MIDIExit = exit;
        }

        private byte Scale(byte value, byte high, byte low) {
            if (value == 0)
                return 0;
            
            return (byte)(((high - low) * (value - 1)) / 62 + low);
        }

        public override void MIDIEnter(Signal n) {
            if (n.Pressed) {
                n.Red = Scale(n.Red, _high.Red, _low.Red);
                n.Green = Scale(n.Green, _high.Green, _high.Green);
                n.Blue = Scale(n.Blue, _high.Blue, _high.Blue);
            }

            if (MIDIExit != null)
                MIDIExit(n);
        }
    }
}