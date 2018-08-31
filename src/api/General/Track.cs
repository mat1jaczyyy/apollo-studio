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
            Chain = new Chain(MIDIExit);

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
        }

        private void WaitForIdentification(object sender, in SysExMessage e) {
            type = Launchpad.AttemptIdentify(e);
            if (type != Launchpad.Type.Unknown) {
                input.SysEx -= WaitForIdentification;
                input.NoteOn += NoteOn;
                input.NoteOff += NoteOff;

                Console.WriteLine($"\nLaunchpad detected: {type.ToString()}\n");
            }
        }

        private void NoteOn(object sender, in NoteOnMessage e) {
            MIDIEnter(new Signal(e.Key, new Color((byte)(e.Velocity >> 1))));
        }

        private void NoteOff(object sender, in NoteOffMessage e) {
            MIDIEnter(new Signal(e.Key, new Color(0)));
        }

        private void MIDIExit(Signal n) {
            if (api.Program.log)
                Console.WriteLine($"OUT <- {n.ToString()}");

            SysExMessage msg = Launchpad.AssembleMessage(n, type);
            output.Send(in msg);
        }

        private void MIDIEnter(Signal n) {
            if (api.Program.log)
                Console.WriteLine($"IN  -> {n.ToString()}");

            Chain.MIDIEnter(n);
        }
    }
}