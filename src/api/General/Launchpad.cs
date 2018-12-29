using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

namespace api {
    public class Launchpad {
        public static readonly string Identifier = "launchpad";

        private IMidiInputDevice Input;
        private IMidiOutputDevice Output;
        private string _name;
        private LaunchpadType Type = LaunchpadType.Unknown;
        private bool _available;
        public InputType InputFormat = InputType.XY;

        public string Name {
            get {
                return _name;
            }
        }

        public bool Available {
            get {
                return _available;
            }
        }

        public delegate void ReceiveEventHandler(Signal n);
        public event ReceiveEventHandler Receive;

        public enum LaunchpadType {
            MK2, PRO, CFW, Unknown
        }

        public enum InputType {
            XY, DrumRack
        }

        private readonly static SysExMessage Inquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});

        private LaunchpadType AttemptIdentify(SysExMessage response) {
            if (response.Data.Length != 15)
                return LaunchpadType.Unknown;

            if (response.Data[0] != 0x7E || response.Data[2] != 0x06 || response.Data[3] != 0x02)
                return LaunchpadType.Unknown;

            if (response.Data[4] == 0x00 && response.Data[5] == 0x20 && response.Data[6] == 0x29) { // Manufacturer = Novation
                switch (response.Data[7]) {
                    case 0x69: // Launchpad MK2
                        return LaunchpadType.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (response.Data[12] == 'c' && response.Data[13] == 'f' && response.Data[14] == 'w')
                            return LaunchpadType.CFW;
                        else
                            return LaunchpadType.PRO;
                }
            }

            return LaunchpadType.Unknown;
        }

        private void WaitForIdentification(object sender, in SysExMessage e) {
            Type = AttemptIdentify(e);
            if (Type != LaunchpadType.Unknown) {
                Input.SysEx -= WaitForIdentification;
                Input.NoteOn += NoteOn;
                Input.NoteOff += NoteOff;
            }
        }

        public void Send(Signal n) {
            byte rgb_byte;

            switch (Type) {
                case LaunchpadType.MK2:
                    rgb_byte = 0x18;
                    if (91 <= n.Index && n.Index <= 98)
                        n.Index += 13;
                    break;
                
                case LaunchpadType.PRO:
                case LaunchpadType.CFW:
                    rgb_byte = 0x10;
                    break;
                
                default:
                    throw new ArgumentException("Launchpad not recognized");
            }

            api.Program.Log($"OUT <- {n.ToString()}");

            SysExMessage msg = new SysExMessage(new byte[] {0x00, 0x20, 0x29, 0x02, rgb_byte, 0x0B, n.Index, n.Color.Red, n.Color.Green, n.Color.Blue});
            Output.Send(in msg);
        }

        public Launchpad(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) {
            Input = input.CreateDevice();
            Output = output.CreateDevice();

            _name = input.Name;

            Input.Open();
            Output.Open();

            _available = true;

            Input.SysEx += WaitForIdentification;            
            Output.Send(in Inquiry);
        }

        public Launchpad(string name) {
            _name = name;
            _available = false;
        }

        public void Connect(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) {
            Input = input.CreateDevice();
            Output = output.CreateDevice();

            Input.Open();
            Output.Open();

            _available = true;

            if (Type == LaunchpadType.Unknown) {
                Input.SysEx += WaitForIdentification;
                Output.Send(in Inquiry);
            } else {
                Input.NoteOn += NoteOn;
                Input.NoteOff += NoteOff;
            }
        }

        public void Disconnect() {
            if (Input.IsOpen)
                Input.Close();
            Input.Dispose();

            if (Output.IsOpen)
                Output.Close();
            Output.Dispose();

            _available = false;
        }

        private void HandleMessage(Signal n) {
            if (_available) {
                if (InputFormat == InputType.DrumRack)
                    n.Index = Conversion.DRtoXY[n.Index];
                
                api.Program.Log($"IN  -> {n.ToString()}");

                if (Receive != null)
                    Receive.Invoke(n);
            }
        }

        private void NoteOn(object sender, in NoteOnMessage e) {
            HandleMessage(new Signal((byte)e.Key, new Color((byte)(e.Velocity >> 1))));
        }

        private void NoteOff(object sender, in NoteOffMessage e) {
            HandleMessage(new Signal((byte)e.Key, new Color(0)));
        }

        public static Launchpad Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            foreach (Launchpad launchpad in MIDI.Devices) {
                if (launchpad.Name == data["port"].ToString()) {
                    return launchpad;
                }
            }
            
            Launchpad lp = new Launchpad(data["port"].ToString());
            MIDI.Devices.Add(lp);
            return lp;
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("port");
                        writer.WriteValue(_name);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}