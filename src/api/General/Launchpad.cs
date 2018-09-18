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
        private IMidiInputDevice Input;
        private IMidiOutputDevice Output;
        private string _name;
        private Types Type = Types.Unknown;
        private bool _available;

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

        public enum Types {
            MK2, PRO, CFW, Unknown
        }

        private readonly static SysExMessage Inquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});

        private Types AttemptIdentify(SysExMessage response) {
            if (response.Data.Length != 15)
                return Types.Unknown;

            if (response.Data[0] != 0x7E || response.Data[2] != 0x06 || response.Data[3] != 0x02)
                return Types.Unknown;

            if (response.Data[4] == 0x00 && response.Data[5] == 0x20 && response.Data[6] == 0x29) { // Manufacturer = Novation
                switch (response.Data[7]) {
                    case 0x69: // Launchpad MK2
                        return Types.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (response.Data[12] == 'c' && response.Data[13] == 'f' && response.Data[14] == 'w')
                            return Types.CFW;
                        else
                            return Types.PRO;
                }
            }

            return Types.Unknown;
        }

        private void WaitForIdentification(object sender, in SysExMessage e) {
            Type = AttemptIdentify(e);
            if (Type != Types.Unknown) {
                Input.SysEx -= WaitForIdentification;
                Input.NoteOn += NoteOn;
                Input.NoteOff += NoteOff;
            }
        }

        public void Send(Signal n) {
            byte rgb_byte;

            switch (Type) {
                case Types.MK2:
                    rgb_byte = 0x18;
                    if (91 <= n.Index && n.Index <= 98)
                        n.Index += 13;
                    break;
                
                case Types.PRO:
                case Types.CFW:
                    rgb_byte = 0x10;
                    break;
                
                default:
                    throw new ArgumentException("Launchpad not recognized");
            }

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

        public Launchpad(string name, Types type) {
            _name = name;
            Type = type;

            _available = false;
        }

        public void Connect(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) {
            Input = input.CreateDevice();
            Output = output.CreateDevice();

            Input.Open();
            Output.Open();

            _available = true;
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

        private void NoteOn(object sender, in NoteOnMessage e) {
            if (_available)
                Receive.Invoke(new Signal(e.Key, new Color((byte)(e.Velocity >> 1))));
        }

        private void NoteOff(object sender, in NoteOffMessage e) {
            if (_available)
                Receive.Invoke(new Signal(e.Key, new Color(0)));
        }

        public static Launchpad Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "launchpad") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            foreach (Launchpad launchpad in MIDI.Devices) {
                if (launchpad.Name == data["port"].ToString()) {
                    return launchpad;
                }
            }
            
            Launchpad lp = new Launchpad(data["port"].ToString(), (Types)Enum.Parse(typeof(Types), data["type"].ToString()));
            MIDI.Devices.Add(lp);
            return lp;
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("launchpad");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("port");
                        writer.WriteValue(Input.Name);

                        writer.WritePropertyName("type");
                        writer.WriteValue(Type.ToString());

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}