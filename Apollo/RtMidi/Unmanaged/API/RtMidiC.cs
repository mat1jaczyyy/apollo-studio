using System;
using System.Runtime.InteropServices;

namespace Apollo.RtMidi.Unmanaged.API {
    internal static class RtMidiC {
        private const string LibraryFile = "rtmidi";

        static RtMidiC() {
            if (IntPtr.Size != 8)
                throw new InvalidProgramException("Only 64-bit RtMIDI is supported!");
        }

        internal static class Utility {
            [DllImport(LibraryFile, EntryPoint = "rtmidi_sizeof_rtmidi_api", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int SizeofRtMidiApi();
        }

        [DllImport(LibraryFile, EntryPoint = "rtmidi_get_compiled_api", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetCompiledApi(/* RtMidiApi* */ IntPtr apis, uint apisSize);

        [DllImport(LibraryFile, EntryPoint = "rtmidi_error", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Error(RtMidiErrorType type, string errorString);

        [DllImport(LibraryFile, EntryPoint = "rtmidi_open_port", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void OpenPort(IntPtr device, uint portNumber, string portName);

        [DllImport(LibraryFile, EntryPoint = "rtmidi_open_virtual_port", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void OpenVirtualPort(IntPtr device, string portName);

        [DllImport(LibraryFile, EntryPoint = "rtmidi_close_port", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ClosePort(IntPtr device);

        [DllImport(LibraryFile, EntryPoint = "rtmidi_get_port_count", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint GetPortCount(IntPtr device);

        [DllImport(LibraryFile, EntryPoint = "rtmidi_get_port_name", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal static extern string GetPortName(IntPtr device, uint portNumber);

        internal static class Input {
            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_create_default", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr CreateDefault();

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_create", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr Create(RtMidiApi api, string clientName, uint queueSizeLimit);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_free", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void Free(IntPtr device);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_get_current_api", CallingConvention = CallingConvention.Cdecl)]
            internal static extern RtMidiApi GetCurrentApi(IntPtr device);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_set_callback", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void SetCallback(IntPtr device, RtMidiCallback callback, IntPtr userData);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_cancel_callback", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void CancelCallback(IntPtr device);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_ignore_types", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void IgnoreTypes(IntPtr device, bool midiSysex, bool midiTime, bool midiSense);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_in_get_message", CallingConvention = CallingConvention.Cdecl)]
            internal static extern double GetMessage(IntPtr device, /* unsigned char** */ out IntPtr message, /* size_t* */ ref UIntPtr size);
        }

        internal static class Output {
            [DllImport(LibraryFile, EntryPoint = "rtmidi_out_create_default", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr CreateDefault();

            [DllImport(LibraryFile, EntryPoint = "rtmidi_out_create", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr Create(RtMidiApi api, string clientName);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_out_free", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void Free(IntPtr device);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_out_get_current_api", CallingConvention = CallingConvention.Cdecl)]
            internal static extern RtMidiApi GetCurrentApi(IntPtr device);

            [DllImport(LibraryFile, EntryPoint = "rtmidi_out_send_message", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int SendMessage(IntPtr device, byte[] message, int length);
        }
    }
}