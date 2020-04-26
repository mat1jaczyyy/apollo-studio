using System;

namespace Apollo.RtMidi.Interface.API
{
    internal struct RtMidiWrapper
    {
        IntPtr ptr;
        IntPtr data;

        public readonly bool Ok;
        public readonly string ErrorMessage;
    }
}