using Apollo.Core;
using Apollo.RtMidi.Unmanaged.Devices;

namespace Apollo.RtMidi.Devices {
    public delegate void MidiMessageHandler(MidiMessage msg);
    
    public interface IMidiInputDevice: IMidiDevice {
        event MidiMessageHandler Received;
    }

    public class MidiInputDevice: MidiDevice, IMidiInputDevice {
        private readonly RtMidiInputDevice _inputDevice;

        internal MidiInputDevice(RtMidiInputDevice rtMidiInputDevice, string name)
        : base(rtMidiInputDevice, name) {
            _inputDevice = rtMidiInputDevice;
            _inputDevice.Message += RtMidiInputDevice_Message;
        }
        
        public event MidiMessageHandler Received;

        private void RtMidiInputDevice_Message(byte[] message) {
            if (message == null) {
                Program.Log("Received null message from device");
                return;
            }

            if (message.Length == 0) {
                Program.Log("Received empty message from device");
                return;
            }

            // TODO(micdah) Decode and propagate midi events on separate thread?
            Received?.Invoke(new MidiMessage(message));
        }
        
        protected override void Disposing() {
            _inputDevice.Message -= RtMidiInputDevice_Message;
            Received = null;
        }
    }
}
