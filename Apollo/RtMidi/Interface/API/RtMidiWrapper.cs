using System;

namespace Apollo.RtMidi.Interface.API
{
    internal struct RtMidiWrapper
    {
        #pragma warning disable CS0169, CS0649
        IntPtr ptr;
        IntPtr data;

        public readonly bool Ok;
        public readonly string ErrorMessage;
        #pragma warning restore CS0169, CS0649
    }
}