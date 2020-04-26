using System;
using Apollo.RtMidi.Interface.Devices;
using Serilog;

namespace Apollo.RtMidi.Devices
{
    public class MidiInputDevice: MidiDevice, IMidiInputDevice
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MidiInputDevice>();
        private readonly RtMidiInputDevice _inputDevice;

        internal MidiInputDevice(RtMidiInputDevice rtMidiInputDevice, string name) : base(rtMidiInputDevice, name)
        {
            _inputDevice = rtMidiInputDevice;
            _inputDevice.Message += RtMidiInputDevice_Message;
        }
        
        public event MidiMessageHandler Received;

        private void RtMidiInputDevice_Message(object sender, byte[] message)
        {
            if (message == null)
            {
                Log.Error("Received null message from device");
                return;
            }

            if (message.Length == 0) 
            {
                Log.Error("Received empty message from device");
                return;
            }

            // TODO Decode and propagate midi events on separate thread as not to block receiving thread

            try 
            {
                Received?.Invoke(new MidiMessage(message));
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception occurred while decoding midi message");
            }
        }
        
        protected override void Disposing()
        {
            _inputDevice.Message -= RtMidiInputDevice_Message;
            
            // Clear all subscribers
            Received = null;
        }
    }
}
