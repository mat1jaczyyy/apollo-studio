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
        private Launchpad.Type type;

        public Track() {
            var inputs = MidiDeviceManager.Default.InputDevices.ToArray();
            Console.WriteLine("\nSelect input for Track:");

            for (int i = 0; i < inputs.Length; i++)
                Console.WriteLine($"{i}. {inputs[i].Name}");
            
            int answer;
            while (!int.TryParse(Console.ReadLine(), out answer));

            input = inputs[answer].CreateDevice();
            input.SysEx += WaitForIdentification;
            input.Open();
            Console.WriteLine($"Selected input: {input.Name}");

            var outputs = MidiDeviceManager.Default.OutputDevices.ToArray();
            Console.WriteLine("\nSelect output for Track:");

            for (int i = 0; i < outputs.Length; i++)
                Console.WriteLine($"{i}. {outputs[i].Name}");
            
            while (!int.TryParse(Console.ReadLine(), out answer));

            output = outputs[answer].CreateDevice();
            output.Open();
            Console.WriteLine($"Selected output: {output.Name}\n");

            output.Send(in Launchpad.Inquiry);

            Chain = new Chain(MIDIExit);
        }

        private void WaitForIdentification(object sender, in SysExMessage e) {
            type = Launchpad.AttemptIdentify(e);
            if (type != Launchpad.Type.Unknown) {
                input.SysEx -= WaitForIdentification;
                input.NoteOn += MIDIEnter;
                
                Console.WriteLine($"\nLaunchpad detected: {type.ToString()}\n");
            }
        }

        private void MIDIExit(Signal n) {
            SysExMessage msg = Launchpad.AssembleMessage(n, type);
            
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