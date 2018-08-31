using System;
using RtMidi.Core.Messages;

namespace api {
    public static class Identification {
        public enum Launchpad {
            MK2, PRO, CFW, Unknown
        }

        public readonly static SysExMessage Inquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});

        public static Launchpad AttemptIdentify(SysExMessage response) {
            // Waiting on https://github.com/micdah/RtMidi.Core/pull/17
            if (response.Data.Length != 15)
                return Launchpad.Unknown;
            
            if (response.Data[0] != 0x7E || response.Data[2] != 0x06 || response.Data[3] != 0x02)
                return Launchpad.Unknown;

            if (response.Data[4] == 0x00 && response.Data[5] == 0x20 && response.Data[6] == 0x29) { // Manufacturer = Novation
                switch (response.Data[7]) {
                    case 0x69: // Launchpad MK2
                        return Launchpad.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (response.Data[12] == 'c' && response.Data[13] == 'f' && response.Data[14] == 'w')
                            return Launchpad.CFW;
                        else
                            return Launchpad.PRO;
                }
            }

            return Launchpad.Unknown;
        }
    }
}