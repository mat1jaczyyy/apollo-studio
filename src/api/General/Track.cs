using System;
using System.Collections.Generic;
using System.Linq;
using api.Devices;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

namespace api {
    public class Track {
        public Chain Chain;
        private IMidiInputDevice input;
        private IMidiOutputDevice output;

        public Track() {
            var inputs = MidiDeviceManager.Default.InputDevices.ToArray();
            Console.WriteLine("\nSelect input for Track:");

            for (int i = 0; i < inputs.Length; i++)
                Console.WriteLine($"{i}. {inputs[i].Name}");
            
            int answer = 0;
            //while (!int.TryParse(Console.ReadLine(), out answer));

            input = inputs[answer].CreateDevice();
            input.NoteOn += MIDIEnter;
            input.SysEx += WaitForIdentification; // TODO: Doesn't actually work. https://github.com/micdah/RtMidi.Core/issues/16
            input.Open();
            Console.WriteLine($"Selected input: {input.Name}");

            var outputs = MidiDeviceManager.Default.OutputDevices.ToArray();
            Console.WriteLine("\nSelect output for Track:");

            for (int i = 0; i < outputs.Length; i++)
                Console.WriteLine($"{i}. {outputs[i].Name}");
            
            //while (!int.TryParse(Console.ReadLine(), out answer));

            output = outputs[answer].CreateDevice();
            output.Open();
            Console.WriteLine($"Selected output: {output.Name}");

            output.Send(in Identification.Inquiry);

            Chain = new Chain(MIDIExit);
        }

        private void WaitForIdentification(object sender, in SysExMessage e) {
            Identification.Launchpad type = Identification.Identify(e);
        }

        private void MIDIExit(Signal n) {
            byte[] data = {0x00, 0x20, 0x29, 0x02, 0x18, 0x0B, n.Index, n.Color.Red, n.Color.Green, n.Color.Blue};
            SysExMessage msg = new SysExMessage(data);
            
            if (api.Program.log)
                Console.WriteLine($"OUT <- {msg.ToString()}");

            output.Send(in msg);
        }

        private void MIDIEnter(object sender, in NoteOnMessage e) {
            if (api.Program.log)
                Console.WriteLine($"IN  -> {e.Key.ToString()} {e.Velocity.ToString()}");

            Chain.MIDIEnter(new Signal(e.Key, new Color((byte)(e.Velocity >> 1))));
        }
    }
}