using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Velocity: Device {
        private int _r, _g, _b;

        public int Red {
            get {
                return _r;
            }
            set {
                if (0 <= value && value <= 63) _r = value;
            }
        }

        public int Green {
            get {
                return _g;
            }
            set {
                if (0 <= value && value <= 63) _g = value;
            }
        }

        public int Blue {
            get {
                return _b;
            }
            set {
                if (0 <= value && value <= 63) _b = value;
            }
        }

        public override Device Clone() {
            return new Velocity(_r, _g, _b);
        }

        public Velocity() {
            this._r = 63;
            this._g = 63;
            this._b = 63;
            this.MIDIExit = null;
        }

        public Velocity(int red, int green, int blue) {
            this._r = red;
            this._g = green;
            this._b = blue;
            this.MIDIExit = null;
        }
        
        public Velocity(Action<Signal> exit) {
            this._r = 63;
            this._g = 63;
            this._b = 63;
            this.MIDIExit = exit;
        }
        
        public Velocity(int red, int green, int blue, Action<Signal> exit) {
            this._r = red;
            this._g = green;
            this._b = blue;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (n.Red != 0 || n.Green != 0 || n.Blue != 0) {
                n.Red = (byte)this._r;
                n.Green = (byte)this._g;
                n.Blue = (byte)this._b;
            }

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }
}