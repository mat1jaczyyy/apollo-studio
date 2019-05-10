using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Move: Device {
        public static readonly new string DeviceIdentifier = "move";

        public Offset Offset;
        public bool Loop;

        public override Device Clone() => new Move(Offset.Clone());

        public Move(Offset offset = null, bool loop = false): base(DeviceIdentifier) {
            Offset = offset?? new Offset();
            Loop = loop;
        }

        private bool ApplyOffset(int index, out int result) {
            int x = index % 10 + Offset.X;
            int y = index / 10 + Offset.Y;

            if (Loop) {
                x = (x + 10) % 10;
                y = (y + 10) % 10;
            }

            result = y * 10 + x;

            if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 98 && result != 9 && result != 90)
                return true;
            
            if (y == -1 && 4 <= x && x <= 5) {
                result = 99;
                return true;
            }
             
            return false;
        }

        public override void MIDIEnter(Signal n) {
            if (n.Index == 99) {
                MIDIExit?.Invoke(n);
                return;
            }

            if (ApplyOffset(n.Index, out int result)) {
                n.Index = (byte)result;
                MIDIExit?.Invoke(n);
            }
        }
    }
}