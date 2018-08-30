using System;
using RtMidi.Core.Messages;

namespace api {
    public static class Identification {
        public enum Launchpad {
            MK2, PRO, CFW, Unknown
        }

        public readonly static SysExMessage Inquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});

        public static Launchpad Identify(SysExMessage response) {
            // TODO: Implement. Waiting on https://github.com/micdah/RtMidi.Core/issues/16

            foreach (byte x in response.Data)
                Console.WriteLine(x);

            return Launchpad.Unknown;
        }
    }
}