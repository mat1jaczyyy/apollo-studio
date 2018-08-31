using System;
using RtMidi.Core.Messages;

namespace api {
    public static class Launchpad {
        public enum Type {
            MK2, PRO, CFW, Unknown
        }

        public readonly static SysExMessage Inquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});

        public static Type AttemptIdentify(SysExMessage response) {
            // Waiting on https://github.com/micdah/RtMidi.Core/pull/17
            if (response.Data.Length != 15)
                return Type.Unknown;
            
            if (response.Data[0] != 0x7E || response.Data[2] != 0x06 || response.Data[3] != 0x02)
                return Type.Unknown;

            if (response.Data[4] == 0x00 && response.Data[5] == 0x20 && response.Data[6] == 0x29) { // Manufacturer = Novation
                switch (response.Data[7]) {
                    case 0x69: // Launchpad MK2
                        return Type.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (response.Data[12] == 'c' && response.Data[13] == 'f' && response.Data[14] == 'w')
                            return Type.CFW;
                        else
                            return Type.PRO;
                }
            }

            return Type.Unknown;
        }

        public static SysExMessage AssembleMessage(Signal n, Type type) {
            byte rgb_byte;

            switch (type) {
                case Type.MK2:
                    rgb_byte = 0x18;
                    if (91 <= n.Index && n.Index <= 98)
                        n.Index += 13;
                    break;
                
                case Type.PRO:
                case Type.CFW:
                    rgb_byte = 0x10;
                    break;
                
                default:
                    throw new ArgumentException("Launchpad not recognized");
            }

            return new SysExMessage(new byte[] {0x00, 0x20, 0x29, 0x02, rgb_byte, 0x0B, n.Index, n.Color.Red, n.Color.Green, n.Color.Blue});
        }
    }
}