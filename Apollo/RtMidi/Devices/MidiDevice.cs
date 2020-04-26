using Apollo.RtMidi.Interface.Devices;
using System;

namespace Apollo.RtMidi.Devices
{

    public abstract class MidiDevice : IMidiDevice 
    {
        private readonly RtMidiDevice _rtMidiDevice;
        private bool _disposed;

        internal MidiDevice(RtMidiDevice rtMidiDevice, string name)
        {
            _rtMidiDevice = rtMidiDevice ?? throw new ArgumentNullException(nameof(rtMidiDevice));
            Name = name;
        }

        public bool IsOpen => _rtMidiDevice.IsOpen;
        public string Name { get; }
        public bool Open() => _rtMidiDevice.Open();
        public void Close() => _rtMidiDevice.Close();
        
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                Disposing();
                _rtMidiDevice.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }

        protected virtual void Disposing()
        {
        }
    }
}
