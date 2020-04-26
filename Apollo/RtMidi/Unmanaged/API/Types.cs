using System;
using System.Runtime.InteropServices;

namespace Apollo.RtMidi.Unmanaged.API {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void RtMidiCallback(double timestamp, IntPtr message, UIntPtr messageSize, IntPtr userData);

    public enum RtMidiApi {
        RT_MIDI_API_UNSPECIFIED,  // Search for a working compiled API
        RT_MIDI_API_MACOSX_CORE,  // Macintosh OS-X Core Midi API.
        RT_MIDI_API_LINUX_ALSA,   // The Advanced Linux Sound Architecture API.
        RT_MIDI_API_UNIX_JACK,    // The Jack Low-Latency MIDI Server API.
        RT_MIDI_API_WINDOWS_MM,   // The Microsoft Multimedia MIDI API.
        RT_MIDI_API_WINDOWS_KS,   // The Microsoft Kernel Streaming MIDI API.
        RT_MIDI_API_RTMIDI_DUMMY  // A compilable but non-functional API.
    }

    public class RtMidiApiException: Exception {
        public RtMidiApiException(string message)
        : base(message) {}
    }

    public enum RtMidiErrorType {
        RT_ERROR_WARNING,
        RT_ERROR_DEBUG_WARNING,
        RT_ERROR_UNSPECIFIED,
        RT_ERROR_NO_DEVICES_FOUND,
        RT_ERROR_INVALID_DEVICE,
        RT_ERROR_MEMORY_ERROR,
        RT_ERROR_INVALID_PARAMETER,
        RT_ERROR_INVALID_USE,
        RT_ERROR_DRIVER_ERROR,
        RT_ERROR_SYSTEM_ERROR,
        RT_ERROR_THREAD_ERROR
    }

    internal struct RtMidiWrapper {
        #pragma warning disable CS0169, CS0649
        IntPtr ptr;
        IntPtr data;

        public readonly bool Ok;
        public readonly string ErrorMessage;
        #pragma warning restore CS0169, CS0649
    }
}