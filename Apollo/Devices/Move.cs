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

        public override void MIDIEnter(Signal n) {
            if (n.Index == 99) {
                MIDIExit?.Invoke(n);
                return;
            }

            int x = n.Index % 10 + Offset.X;
            int y = n.Index / 10 + Offset.Y;

            if (Loop) {
                x = (x + 10) % 10;
                y = (y + 10) % 10;
            }

            int result = y * 10 + x;
                
            if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 98 && result != 9 && result != 90) {
                n.Index = (byte)result;
                MIDIExit?.Invoke(n);

            } else if (y == -1 && 4 <= x && x <= 5) {
                n.Index = 99;
                MIDIExit?.Invoke(n);
            }
        }
    }
}