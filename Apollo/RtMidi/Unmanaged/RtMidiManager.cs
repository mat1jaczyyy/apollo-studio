using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Apollo.RtMidi.Unmanaged.Devices;
using Apollo.RtMidi.Unmanaged.Devices.Infos;
using Apollo.RtMidi.Unmanaged.API;

namespace Apollo.RtMidi.Unmanaged {
    internal class RtMidiManager: IDisposable {
        public static RtMidiManager Default => DefaultHolder.Value;

        public static readonly Lazy<RtMidiManager> DefaultHolder = new(() => new RtMidiManager());

        readonly RtMidiInputDevice _defaultInputDevice;
        readonly RtMidiOutputDevice _defaultOutputDevice;
        bool _disposed;

        RtMidiManager() {
            // These are used exclusively to get number of available ports for the given
            // type of device (input/output) as well as to provide port names
            _defaultInputDevice = new RtMidiInputDevice(0);
            _defaultOutputDevice = new RtMidiOutputDevice(0);
        }

        ~RtMidiManager() => Dispose();

        public IEnumerable<RtMidiInputDeviceInfo> InputDevices {
            get {
                for (uint port = 0; port < _defaultInputDevice.GetPortCount(); port++)
                    yield return new RtMidiInputDeviceInfo(port, _defaultInputDevice.GetPortName(port));
            }
        }

        public IEnumerable<RtMidiOutputDeviceInfo> OutputDevices {
            get  {
                for (uint port = 0; port < _defaultOutputDevice.GetPortCount(); port++)
                    yield return new RtMidiOutputDeviceInfo(port, _defaultOutputDevice.GetPortName(port));
            }
        }

        public static IEnumerable<RtMidiApi> GetAvailableApis() {
            IntPtr apisPtr = IntPtr.Zero;

            try {
                // Get number of APIs
                int count = RtMidiC.GetCompiledApi(IntPtr.Zero, 0);
                if (count <= 0)
                    return new RtMidiApi[0];

                // Get array of available APIs
                int enumSize = RtMidiC.Utility.SizeofRtMidiApi();
                apisPtr = Marshal.AllocHGlobal(count * enumSize);
                RtMidiC.GetCompiledApi(apisPtr, (uint)count);

                // Convert to managed Enum types
                switch (enumSize) {
                    case 1:
                        byte[] bytes = new byte[count];
                        Marshal.Copy(apisPtr, bytes, 0, bytes.Length);
                        return bytes.Cast<RtMidiApi>();

                    case 2:
                        short[] shorts = new short[count];
                        Marshal.Copy(apisPtr, shorts, 0, shorts.Length);
                        return shorts.Cast<RtMidiApi>();

                    case 4:
                        int[] ints = new int[count];
                        Marshal.Copy(apisPtr, ints, 0, ints.Length);
                        return ints.Cast<RtMidiApi>();

                    case 8:
                        long[] longs = new long[count];
                        Marshal.Copy(apisPtr, longs, 0, longs.Length);
                        return longs.Cast<RtMidiApi>();

                    default:
                        throw new NotSupportedException($"Unexpected size of RtMidiApi enum {enumSize}");
                }

            } catch (Exception) {
                Console.Error.WriteLine("Unexpected exception occurred while listing available RtMidi APIs");
                return new RtMidiApi[0];

            } finally {
                if (apisPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(apisPtr);
            }
        }

        public void Dispose() {
            if (_disposed) return;

            _defaultInputDevice.Dispose();
            _defaultOutputDevice.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
