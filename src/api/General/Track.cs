using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api.Devices;

namespace api {
    public class Track: IChainParent {
        public static readonly string Identifier = "track";
        
        public int? ParentIndex;

        public Chain Chain;
        private Launchpad _launchpad;
        private Pixel[] screen = new Pixel[128];

        public Launchpad Launchpad {
            get {
                return _launchpad;
            }
            set {
                if (_launchpad != null)
                    _launchpad.Receive -= MIDIEnter;

                _launchpad = value;

                if (_launchpad != null)
                    _launchpad.Receive += MIDIEnter;
            }
        }

        public Track(Chain init = null, Launchpad launchpad = null) {
            if (init == null) init = new Chain();
            Chain = init;
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;
            
            for (int i = 0; i < 128; i++)
                screen[i] = new Pixel() {MIDIExit = MIDIExit};

            Launchpad = launchpad;
        }

        private void ChainExit(Signal n) {
            screen[n.Index].MIDIEnter(n);
        }

        private void MIDIExit(Signal n) {
            Launchpad.Send(n);
        }

        private void MIDIEnter(Signal n) {
            if (Chain != null)
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
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Track(Chain.Decode(data["chain"].ToString()), Launchpad.Decode(data["launchpad"].ToString()));
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

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

        public string Request(string type, Dictionary<string, object> content) => Set.Request("forward", new Dictionary<string, object>() {
            ["forward"] = Identifier,
            ["index"] = ParentIndex,
            ["message"] = Communication.UI.EncodeMessage(Identifier, type, content)
        });

        public ObjectResult Respond(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != Identifier) return new BadRequestObjectResult("Incorrect recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    switch (data["forward"].ToString()) {
                        case "chain":
                            return Chain.Respond(data["message"].ToString());
                        
                        default:
                            return new BadRequestObjectResult("Incorrectly formatted message.");
                    }
                
                case "port":
                    Launchpad = MIDI.Devices[Convert.ToInt32(data["index"])];
                    return new OkObjectResult(Launchpad.Encode());
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}