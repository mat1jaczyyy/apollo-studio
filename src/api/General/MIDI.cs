using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace api {
    public static class MIDI {
        public static readonly string Identifier = "midi";

        public static List<Launchpad> Devices = new List<Launchpad>();

        public static void Rescan() {
            /*
              The idea here is that we update Devices as MIDI devices are plugged in or out.
            RtMidi doesn't provide any kind of callback function for reporting changes,
            so we must scan manually every x milliseconds and check for changes ourselves.
            The first attempt was using an async Threading.Timer, which always returned the
            same data instead of updated devices. The second attempt used an async Task that
            would have its thread sleep, but this caused segfaults when the app is idling.
            So, for the time being, this function will have to be called manually.

              https://github.com/micdah/RtMidi.Core/issues/18
            */

            foreach (var input in MidiDeviceManager.Default.InputDevices) {
                foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                    if (input.Name == output.Name) {
                        bool justConnected = true;

                        foreach (Launchpad device in Devices) {
                            if (device.Name == output.Name) {
                                if (!device.Available) {
                                    device.Connect(input, output);
                                }
                                justConnected = false;
                                break;
                            }
                        }

                        if (justConnected) {
                            Devices.Add(new Launchpad(input, output));
                        }
                    }
                }
            }

            foreach (Launchpad device in Devices) {
                if (device.Available) {
                    bool justDisconnected = true;

                    foreach (var output in MidiDeviceManager.Default.OutputDevices) {
                        if (device.Name == output.Name) {
                            justDisconnected = false;
                            break;
                        }
                    }

                    if (justDisconnected) {
                        device.Disconnect();
                    }
                }
            }
        }

        public static string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < Devices.Count; i++) {
                            writer.WriteRawValue(Devices[i].Encode());
                        }

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }

            return json.ToString();
        }

        public static ObjectResult Respond(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != Identifier) return new BadRequestObjectResult("Incorrect recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    return new BadRequestObjectResult("The MIDI object has no members to forward to.");

                case "rescan":
                    Rescan();
                    return new OkObjectResult(Encode());

                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}
