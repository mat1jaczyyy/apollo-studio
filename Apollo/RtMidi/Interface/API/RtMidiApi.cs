using System;

namespace Apollo.RtMidi.Interface.API
{
    public enum RtMidiApi
    {
        /// <summary>
        /// Search for a working compiled API
        /// </summary>
        RT_MIDI_API_UNSPECIFIED,

        /// <summary>
        /// Macintosh OS-X Core Midi API.
        /// </summary>
        RT_MIDI_API_MACOSX_CORE,

        /// <summary>
        /// The Advanced Linux Sound Architecture API.
        /// </summary>
        RT_MIDI_API_LINUX_ALSA,

        /// <summary>
        /// The Jack Low-Latency MIDI Server API.
        /// </summary>
        RT_MIDI_API_UNIX_JACK,

        /// <summary>
        /// The Microsoft Multimedia MIDI API.
        /// </summary>
        RT_MIDI_API_WINDOWS_MM,

        /// <summary>
        /// The Microsoft Kernel Streaming MIDI API.
        /// </summary>
        RT_MIDI_API_WINDOWS_KS,

        /// <summary>
        /// A compilable but non-functional API.
        /// </summary>
        RT_MIDI_API_RTMIDI_DUMMY
    }

    public class RtMidiApiException : Exception
    {
        public RtMidiApiException(string message) : base(message)
        {
        }
    }
}