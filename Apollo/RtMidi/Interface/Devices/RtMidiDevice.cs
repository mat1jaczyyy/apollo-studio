using System;
using Apollo.Core;
using Apollo.RtMidi.Interface.API;
using System.Runtime.InteropServices;
namespace Apollo.RtMidi.Interface.Devices
{

    /// <summary>
    /// Abstract RtMidi device base class
    /// </summary>
    internal abstract class RtMidiDevice
    {
        private IntPtr _handle;
        private readonly uint _portNumber;
        private bool _disposed;
        private bool _isOpen;

        protected RtMidiDevice(uint portNumber) 
        {
            _handle = IntPtr.Zero;
            _portNumber = portNumber;
        }

        ~RtMidiDevice() 
        {
            // Ensure Interface handles are freed
            Dispose();
        }

        public bool IsOpen => _isOpen;

        /// <summary>
        /// Read-only access to the Interface device handle
        /// </summary>
        protected IntPtr Handle => _handle;

        public bool Open() 
        {
            if (_isOpen) return false;

            if (!EnsureDevice())
            {
                return false;
            }

            try
            {
                Program.DebugLog($"Fetching port name, for port {_portNumber}");
                var portName = RtMidiC.GetPortName(_handle, _portNumber);
                CheckForError();

                Program.DebugLog($"Opening port {_portNumber} using name {portName}");
                RtMidiC.OpenPort(_handle, _portNumber, portName);
                CheckForError();

                _isOpen = true;

                return true;
            }
            catch (Exception) 
            {
                Program.Log($"Unable to open port number {_portNumber}");
                return false;
            }
        }

        public void Close()
        {
            if (!_isOpen) return;

            try
            {
                Program.DebugLog($"Closing port number {_portNumber}");
                RtMidiC.ClosePort(_handle);
                CheckForError();

                _isOpen = false;
            }
            catch (Exception)
            {
                Program.Log($"Unable to close port number {_portNumber}");
            }
        }

        /// <summary>
        /// Get number of available ports for this device type
        /// </summary>
        /// <returns>Number of ports</returns>
        internal uint GetPortCount()
        {
            if (!EnsureDevice()) return 0;

            try
            {
                var count = RtMidiC.GetPortCount(_handle);
                CheckForError();
                return count;
            }
            catch (Exception)
            {
                Program.Log("Error while getting number of ports");
                return 0;
            }
        }

        /// <summary>
        /// Get name of port, for this device type
        /// </summary>
        /// <returns>The port name.</returns>
        /// <param name="portNumber">Port number.</param>
        internal string GetPortName(uint portNumber) 
        {
            if (!EnsureDevice()) return null;

            try 
            {
                var name = RtMidiC.GetPortName(_handle, portNumber);
                CheckForError();
                return name;
            }
            catch (Exception)
            {
                Program.Log($"Error while getting port {portNumber} name");
                return null;
            }
        }

        private bool EnsureDevice()
        {
            if (_handle != IntPtr.Zero) return true;

            _handle = CreateDevice();

            return _handle != IntPtr.Zero;
        }

        protected void CheckForError()
        {
            CheckForError(_handle);
        }

        protected static void CheckForError(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;

            var wrapper = (RtMidiWrapper)Marshal.PtrToStructure(handle, typeof(RtMidiWrapper));
            if (!wrapper.Ok)
            {
                Program.Log($"Error detected from RtMidi API '{wrapper.ErrorMessage}'");
                throw new RtMidiApiException($"Error detected from RtMidi API '{wrapper.ErrorMessage}'");
            }
        }

        public void Dispose() 
        {
            if (_disposed) return;

            // Ensure device is closed
            if (_isOpen) 
            {
                Close();
            }

            // Ensure device is destroyed
            if (_handle != IntPtr.Zero) 
            {
                DestroyDevice();
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        protected abstract IntPtr CreateDevice();
        protected abstract void DestroyDevice();
    }
}
