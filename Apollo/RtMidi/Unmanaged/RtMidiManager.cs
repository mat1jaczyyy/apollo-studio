using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Apollo.RtMidi.Unmanaged.API;
using Apollo.RtMidi.Unmanaged.Devices;
using Apollo.RtMidi.Unmanaged.Devices.Infos;

namespace Apollo.RtMidi.Unmanaged
{
    internal class RtMidiManager : IDisposable
    {
        public static RtMidiManager Default => DefaultHolder.Value;

        public static readonly Lazy<RtMidiManager> DefaultHolder = new Lazy<RtMidiManager>(() => new RtMidiManager());

        private readonly RtMidiInputDevice _defaultInputDevice;
        private readonly RtMidiOutputDevice _defaultOutputDevice;
        private bool _disposed;

        private RtMidiManager()
        {
            /*
             * These are used exlusively to get number of available ports for the given
             * type of device (input/output) as well as to provide port names
             */
            _defaultInputDevice = new RtMidiInputDevice(0);
            _defaultOutputDevice = new RtMidiOutputDevice(0);
        }

        ~RtMidiManager() 
        {
            Dispose();
        }

        /// <summary>
        /// Enumerate all currently available input devices
        /// </summary>
        public IEnumerable<RtMidiInputDeviceInfo> InputDevices
        {
            get
            {
                for (uint port = 0; port < _defaultInputDevice.GetPortCount(); port++)
                {
                    yield return new RtMidiInputDeviceInfo(port, _defaultInputDevice.GetPortName(port));
                }
            }
        }

        /// <summary>
        /// Enumerate all currently available output devices
        /// </summary>
        public IEnumerable<RtMidiOutputDeviceInfo> OutputDevices
        {
            get 
            {
                for (uint port = 0; port < _defaultOutputDevice.GetPortCount(); port++) 
                {
                    yield return new RtMidiOutputDeviceInfo(port, _defaultOutputDevice.GetPortName(port));
                }
            }
        }

        /// <summary>
        /// Get array of available RtMidi API's (if any)
        /// </summary>
        /// <returns>The available apis.</returns>
        public static IEnumerable<RtMidiApi> GetAvailableApis()
        {
            var apisPtr = IntPtr.Zero;

            try
            {
                // Get number of API's
                var count = RtMidiC.GetCompiledApi(IntPtr.Zero, 0);
                if (count <= 0)
                    return new RtMidiApi[0];

                // Get array of available API's
                var enumSize = RtMidiC.Utility.SizeofRtMidiApi();
                apisPtr = Marshal.AllocHGlobal(count * enumSize);
                RtMidiC.GetCompiledApi(apisPtr, (uint)count);

                // Convert to managed enum types
                switch (enumSize)
                {
                    case 1:
                        var bytes = new byte[count];
                        Marshal.Copy(apisPtr, bytes, 0, bytes.Length);
                        return bytes.Select(b => (RtMidiApi)b);
                    case 2:
                        var shorts = new short[count];
                        Marshal.Copy(apisPtr, shorts, 0, shorts.Length);
                        return shorts.Select(s => (RtMidiApi)s);
                    case 4:
                        var ints = new int[count];
                        Marshal.Copy(apisPtr, ints, 0, ints.Length);
                        return ints.Select(i => (RtMidiApi)i);
                    case 8:
                        var longs = new long[count];
                        Marshal.Copy(apisPtr, longs, 0, longs.Length);
                        return longs.Select(l => (RtMidiApi)l);
                    default:
                        throw new NotSupportedException($"Unexpected size of RtMidiApi enum {enumSize}");
                }
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Unexpected exception occurred while listing available RtMidi API's");

                return new RtMidiApi[0];
            }
            finally
            {
                if (apisPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(apisPtr);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _defaultInputDevice.Dispose();
            _defaultOutputDevice.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
