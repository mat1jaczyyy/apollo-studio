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

        public Track() {
            Chain = new Chain(ChainExit);

            for (int i = 0; i < 128; i++) {
                screen[i] = new Pixel(MIDIExit);
            }
        }

        public Track(Chain init) {
            Chain = init;
            Chain.MIDIExit = ChainExit;
            
            for (int i = 0; i < 128; i++) {
                screen[i] = new Pixel(MIDIExit);
            }
        }

        public Track(Launchpad launchpad) {
            Chain = new Chain(ChainExit);

            for (int i = 0; i < 128; i++) {
                screen[i] = new Pixel(MIDIExit);
            }

            Launchpad = launchpad;
            Launchpad.Receive += MIDIEnter;
        }

        public Track(Chain init, Launchpad launchpad) {
            Chain = init;
            Chain.MIDIExit = ChainExit;
            
            for (int i = 0; i < 128; i++) {
                screen[i] = new Pixel(MIDIExit);
            }

            Launchpad = launchpad;
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

        public void Dispose() {
            if (Launchpad != null)
                Launchpad.Receive -= MIDIEnter;
            
            Chain = null;
        }

        public override string ToString() {
            return Launchpad.Name;
        }
        
        public static Track Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "track") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Track(Chain.Decode(data["chain"].ToString()), Launchpad.Decode(data["launchpad"].ToString()));
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