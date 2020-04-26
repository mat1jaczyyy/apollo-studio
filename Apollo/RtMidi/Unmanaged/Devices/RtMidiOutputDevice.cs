using System;

using Apollo.Core;
using Apollo.RtMidi.Unmanaged.API;

namespace Apollo.RtMidi.Unmanaged.Devices {
    internal class RtMidiOutputDevice: RtMidiDevice {
        internal RtMidiOutputDevice(uint portNumber): base(portNumber) {}

        public bool SendMessage(byte[] message) {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Cannot send if device is not open
            if (!IsOpen) return false;

            try {
                int result = RtMidiC.Output.SendMessage(Handle, message, message.Length);
                CheckForError();
                return result == 0;

            } catch (Exception) {
                Program.Log("Error while sending message");
                return false;
            }
        }

        protected override IntPtr CreateDevice() {
            try {
                IntPtr handle = RtMidiC.Output.CreateDefault();
                CheckForError(handle);
                return handle;

            } catch (Exception) {
                Program.Log("Unable to create default output device");
                return IntPtr.Zero;
            }
        }

        protected override void DestroyDevice() {
            try {
                Program.DebugLog("Freeing output device handle");
                RtMidiC.Output.Free(Handle);
                CheckForError();
                
            } catch (Exception) {
                Program.Log("Error while freeing output device handle");
            }
        }
    }
}
