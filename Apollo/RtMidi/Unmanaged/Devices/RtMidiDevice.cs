using System;
using System.Runtime.InteropServices;

using Apollo.Core;
using Apollo.RtMidi.Unmanaged.API;

namespace Apollo.RtMidi.Unmanaged.Devices {
    internal abstract class RtMidiDevice {
        protected IntPtr Handle { get; private set; }
        readonly uint _portNumber;
        bool _disposed;
        public bool IsOpen { get; private set; }

        protected RtMidiDevice(uint portNumber) {
            Handle = IntPtr.Zero;
            _portNumber = portNumber;
        }

        // Ensure Interface handles are freed
        ~RtMidiDevice() => Dispose();

        public bool Open() {
            if (IsOpen) return false;

            if (!EnsureDevice()) return false;

            try {
                Program.DebugLog($"Fetching port name, for port {_portNumber}");
                string portName = RtMidiC.GetPortName(Handle, _portNumber);
                CheckForError();

                Program.DebugLog($"Opening port {_portNumber} using name {portName}");
                RtMidiC.OpenPort(Handle, _portNumber, portName);
                CheckForError();

                IsOpen = true;

                return true;

            } catch (Exception) {
                Program.Log($"Unable to open port number {_portNumber}");
                return false;
            }
        }

        public void Close() {
            if (!IsOpen) return;

            try {
                Program.DebugLog($"Closing port number {_portNumber}");
                RtMidiC.ClosePort(Handle);
                CheckForError();

                IsOpen = false;

            } catch (Exception) {
                Program.Log($"Unable to close port number {_portNumber}");
            }
        }

        internal uint GetPortCount() {
            if (!EnsureDevice()) return 0;

            try {
                uint count = RtMidiC.GetPortCount(Handle);
                CheckForError();
                return count;

            } catch (Exception) {
                Program.Log("Error while getting number of ports");
                return 0;
            }
        }

        internal string GetPortName(uint portNumber)  {
            if (!EnsureDevice()) return null;

            try {
                string name = RtMidiC.GetPortName(Handle, portNumber);
                CheckForError();
                return name;

            } catch (Exception) {
                Program.Log($"Error while getting port {portNumber} name");
                return null;
            }
        }

        bool EnsureDevice() {
            if (Handle != IntPtr.Zero) return true;

            Handle = CreateDevice();

            return Handle != IntPtr.Zero;
        }

        protected void CheckForError()
            => CheckForError(Handle);

        protected static void CheckForError(IntPtr handle) {
            if (handle == IntPtr.Zero) return;

            RtMidiWrapper wrapper = (RtMidiWrapper)Marshal.PtrToStructure(handle, typeof(RtMidiWrapper));

            if (!wrapper.Ok) {
                Program.Log($"Error detected from RtMidi API '{wrapper.ErrorMessage}'");
                throw new RtMidiApiException($"Error detected from RtMidi API '{wrapper.ErrorMessage}'");
            }
        }

        public void Dispose() {
            if (_disposed) return;

            if (IsOpen)
                Close();

            if (Handle != IntPtr.Zero) 
                DestroyDevice();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        protected abstract IntPtr CreateDevice();
        protected abstract void DestroyDevice();
    }
}
