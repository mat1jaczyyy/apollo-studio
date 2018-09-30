using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

namespace api {
    public static class MIDI {
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
    }
}