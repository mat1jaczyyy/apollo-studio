using System;
using Apollo.RtMidi.Interface.API;
using Serilog;
using System.Runtime.InteropServices;
namespace Apollo.RtMidi.Interface.Devices
{
    internal class RtMidiInputDevice : RtMidiDevice
    {
        /// <summary>
        /// Ensure delegate is not garbage collected (see https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c)
        /// </summary>
        private readonly RtMidiCallback _rtMidiCallbackDelegate;

        internal RtMidiInputDevice(uint portNumber) : base(portNumber)
        {
            _rtMidiCallbackDelegate = HandleRtMidiCallback;
        }

        public event EventHandler<byte[]> Message;

        protected override IntPtr CreateDevice()
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                Log.Debug("Creating default input device");
                handle = RtMidiC.Input.CreateDefault();
                CheckForError(handle);

                Log.Debug("Setting types to ignore");
                RtMidiC.Input.IgnoreTypes(handle, false, true, true);
                CheckForError(handle);

                Log.Debug("Setting input callback");
                RtMidiC.Input.SetCallback(handle, _rtMidiCallbackDelegate, IntPtr.Zero);
                CheckForError(handle);

                return handle;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unable to create default input device");

                if (handle != IntPtr.Zero)
                {
                    Log.Information("Freeing input device handle");
                    try
                    {
                        RtMidiC.Input.Free(handle);
                        CheckForError(handle);
                    }
                    catch (Exception e2)
                    {
                        Log.Error(e2, "Unable to free input device");
                    }
                }

                return IntPtr.Zero;
            }
        }

        private void HandleRtMidiCallback(double timestamp, IntPtr messagePtr, UIntPtr messageSize, IntPtr userData)
        {
            try
            {
                var messageHandlers = Message;
                if (messageHandlers != null)
                {
                    // Copy message to managed byte array
                    var size = (int)messageSize;
                    var message = new byte[size];
                    Marshal.Copy(messagePtr, message, 0, size);

                    // Invoke message handlers
                    messageHandlers.Invoke(this, message);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception occurred while receiving MIDI message");
                return;
            }


        }

        protected override void DestroyDevice()
        {
            try
            {
                Log.Debug("Cancelling input callback");
                RtMidiC.Input.CancelCallback(Handle);
                CheckForError();

                Log.Debug("Freeing input device handle");
                RtMidiC.Input.Free(Handle);
                CheckForError();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while freeing input device handle");
            }
        }
    }
}
