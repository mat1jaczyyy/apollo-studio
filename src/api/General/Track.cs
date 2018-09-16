using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using api.Devices;
using Newtonsoft.Json;

namespace api {
    public class Track {
        public Chain Chain;
        public Launchpad Launchpad;
        private Pixel[] screen = new Pixel[128];

        public void Refresh() {
            foreach (Launchpad device in MIDI.Devices)
                if (device.Name == Launchpad.Name) {
                    Launchpad = device;
                    Launchpad.Receive += MIDIEnter;
                    return;
                }
        }

        public Track() {
            Chain = new Chain(ChainExit);
            for (int i = 0; i < 128; i++) {
                screen[i] = new Pixel(MIDIExit);
            }

            Launchpad = MIDI.Devices[0];
            Launchpad.Receive += MIDIEnter;
        }

        private void ChainExit(Signal n) {
            screen[n.Index].MIDIEnter(n);
        }

        private void MIDIExit(Signal n) {
            if (api.Program.log)
                Console.WriteLine($"OUT <- {n.ToString()}");

            Launchpad.Send(n);
        }

        private void MIDIEnter(Signal n) {
            if (api.Program.log)
                Console.WriteLine($"IN  -> {n.ToString()}");

            Chain.MIDIEnter(n);
        }

        public override string ToString() {
            return Launchpad.Name;
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("track");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("chain");
                        writer.WriteRawValue(Chain.Encode());

                        writer.WritePropertyName("launchpad");
                        writer.WriteRawValue(Launchpad.Encode());

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}