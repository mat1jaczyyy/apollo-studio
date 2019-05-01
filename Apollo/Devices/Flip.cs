using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Flip: Device {
        public static readonly new string DeviceIdentifier = "flip";

        public enum FlipType {
            Horizontal,
            Vertical,
            Diagonal1,
            Diagonal2
        }

        FlipType _mode;
        public string Mode {
            get {
                if (_mode == FlipType.Horizontal) return "Horizontal";
                else if (_mode == FlipType.Vertical) return "Vertical";
                else if (_mode == FlipType.Diagonal1) return "Diagonal+";
                else if (_mode == FlipType.Diagonal2) return "Diagonal-";
                return null;
            }
            set {
                if (value == "Horizontal") _mode = FlipType.Horizontal;
                else if (value == "Vertical") _mode = FlipType.Vertical;
                else if (value == "Diagonal+") _mode = FlipType.Diagonal1;
                else if (value == "Diagonal-") _mode = FlipType.Diagonal2;
            }
        }

        public FlipType GetFlipMode() => _mode;

        public bool Bypass;

        public override Device Clone() => new Flip(_mode, Bypass);

        public Flip(FlipType mode = FlipType.Horizontal, bool bypass = false): base(DeviceIdentifier) {
            _mode = mode;
            Bypass = bypass;
        }

        public override void MIDIEnter(Signal n) {
            if (Bypass) MIDIExit?.Invoke(n.Clone());
            
            int x = n.Index % 10;
            int y = n.Index / 10;

            if (_mode == FlipType.Horizontal) x = 9 - x;
            else if (_mode == FlipType.Vertical) y = 9 - y;

            else if (_mode == FlipType.Diagonal1) {
                int temp = x;
                x = y;
                y = temp;
            
            } else if (_mode == FlipType.Diagonal2) {
                x = 9 - x;
                y = 9 - y;

                int temp = x;
                x = y;
                y = temp;
            }

            int result = y * 10 + x;
            
            n.Index = (byte)result;
            MIDIExit?.Invoke(n);
        }
    }
}