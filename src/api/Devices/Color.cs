using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Color: Device {
        private byte _rh, _rl, _gh, _gl, _bh, _bl;

        public byte RedHigh {
            get {
                return _rh;
            }
            set {
                if (0 <= value && value <= 63) _rh = value;
            }
        }

        public byte RedLow {
            get {
                return _rl;
            }
            set {
                if (0 <= value && value <= 63) _rl = value;
            }
        }

        public byte GreenHigh {
            get {
                return _gh;
            }
            set {
                if (0 <= value && value <= 63) _gh = value;
            }
        }

        public byte GreenLow {
            get {
                return _gl;
            }
            set {
                if (0 <= value && value <= 63) _gl = value;
            }
        }

        public byte BlueHigh {
            get {
                return _bh;
            }
            set {
                if (0 <= value && value <= 63) _bh = value;
            }
        }

        public byte BlueLow {
            get {
                return _bl;
            }
            set {
                if (0 <= value && value <= 63) _bl = value;
            }
        }

        public override Device Clone() {
            return new Color(_rh, _rl, _gh, _gl, _bh, _bl);
        }

        public Color() {
            _rh = _rl = 63;
            _gh = _gl = 63;
            _bh = _bl = 63;
            MIDIExit = null;
        }

        public Color(byte red, byte green, byte blue) {
            _rh = _rl = red;
            _gh = _gl = green;
            _bh = _bl = blue;
            MIDIExit = null;
        }

        public Color(byte redhigh, byte redlow, byte greenhigh, byte greenlow, byte bluehigh, byte bluelow) {
            _rh = redhigh;   _rl = redlow;
            _gh = greenhigh; _gl = greenlow;
            _bh = bluehigh;  _bl = bluelow;
            MIDIExit = null;
        }
        
        public Color(Action<Signal> exit) {
            _rh = _rl = 63;
            _gh = _gl = 63;
            _bh = _bl = 63;
            MIDIExit = exit;
        }

        public Color(byte red, byte green, byte blue, Action<Signal> exit) {
            _rh = _rl = red;
            _gh = _gl = green;
            _bh = _bl = blue;
            MIDIExit = exit;
        }

        public Color(byte redhigh, byte redlow, byte greenhigh, byte greenlow, byte bluehigh, byte bluelow, Action<Signal> exit) {
            _rh = redhigh;   _rl = redlow;
            _gh = greenhigh; _gl = greenlow;
            _bh = bluehigh;  _bl = bluelow;
            MIDIExit = exit;
        }

        private byte Scale(byte value, byte high, byte low) {
            if (value == 0)
                return 0;
            
            return (byte)(((high - low) * (value - 1)) / 62 + low);
        }

        public override void MIDIEnter(Signal n) {
            if (n.Pressed) {
                n.Red = Scale(n.Red, _rh, _rl);
                n.Green = Scale(n.Green, _gh, _gl);
                n.Blue = Scale(n.Blue, _bh, _bl);
            }

            if (MIDIExit != null)
                MIDIExit(n);
        }
    }
}