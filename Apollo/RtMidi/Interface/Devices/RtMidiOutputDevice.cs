using System;
using Apollo.RtMidi.Interface.API;
using Serilog;
namespace Apollo.RtMidi.Interface.Devices
{
    internal class RtMidiOutputDevice : RtMidiDevice
    {
        internal RtMidiOutputDevice(uint portNumber) : base(portNumber)
        {
        }

        public bool SendMessage(byte[] message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Cannot send, if device is not open
            if (!IsOpen) return false;

            try
            {
                var result = RtMidiC.Output.SendMessage(Handle, message, message.Length);
                CheckForError();
                return result == 0;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while sending message");
                return false;
            }
        }

        protected override IntPtr CreateDevice()
        {
            try
            {
                var handle = RtMidiC.Output.CreateDefault();
                CheckForError(handle);
                return handle;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unable to create default output device");
                return IntPtr.Zero;
            }
        }

        protected override void DestroyDevice()
        {
            try
            {
                Log.Debug("Freeing output device handle");
                RtMidiC.Output.Free(Handle);
                CheckForError();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while freeing output device handle");
            }
        }
    }
}
