using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Lightweight: Device {
        public static readonly new string DeviceIdentifier = "lightweight";

        private string _path = null;
        private BinaryReader _read;
        
        private List<Timer> _timers = new List<Timer>();
        private List<int> _timecodes = new List<int>();
        private List<Signal> _signals = new List<Signal>();
        private TimerCallback _timerexit;

        private int ReadVariableLength() {
            int ret = 0;
            for (int i = 0; i < 4; i++) {
                byte b = _read.ReadByte();
                ret <<= 7;
                ret += (b & 0x7F);
                if (b >> 7 == 0) return ret;
            }
            return ret;
        }

        private void DiscardMeta() {
            switch (_read.ReadByte()) {
                case 0x00: // Sequence number
                case 0x59: // Key signature
                    _read.ReadBytes(3);
                    break;

                case 0x01: // Text event
                case 0x02: // Copyright notice
                case 0x03: // Track name
                case 0x04: // Instrument name
                case 0x05: // Lyric
                case 0x06: // Marker
                case 0x07: // Cue Point
                case 0x7F: // Sequencer specific
                    _read.ReadBytes(ReadVariableLength());
                    break;

                case 0x20: // MIDI channel prefix
                case 0x21: // MIDI Port
                    _read.ReadBytes(2);
                    break;
                
                case 0x2F: // End of Track
                    _read.ReadByte();
                    break;
                
                case 0x51: // Tempo
                    _read.ReadBytes(4);
                    break;
                
                case 0x54: // SMPTE Offset
                    _read.ReadBytes(6);
                    break;
                
                case 0x58: // Time signature
                    _read.ReadBytes(5);
                    break;
            }
        }

        private void DiscardSystem(byte type) {
            switch (type) {
                case 0x0: // SysEx Start
                    _read.ReadBytes(ReadVariableLength());
                    break;
                
                case 0x2: // Song position pointer
                    _read.ReadBytes(2);
                    break;

                case 0x3: // Song select
                    _read.ReadByte();
                    break;
                
                case 0xF: // Meta
                    DiscardMeta();
                    break;
            }
        }

        public string Path {
            get => _path;
            set {
                if (!File.Exists(value))
                    return;
                
                using (_read = new BinaryReader(File.Open(value, FileMode.Open))) {
                    if (!_read.ReadChars(4).SequenceEqual(new char[] {'M', 'T', 'h', 'd'})) // Header Identifier
                        return;
                    
                    if (!_read.ReadBytes(4).SequenceEqual(new byte[] {0x00, 0x00, 0x00, 0x06})) // Header size
                        return;

                    if (!_read.ReadBytes(4).SequenceEqual(new byte[] {0x00, 0x00, 0x00, 0x01})) // Single track file
                        return;
                    
                    _read.ReadBytes(2); // Skip BPM info (usually 0x00, 0x60 = 120BPM)

                    if (!_read.ReadChars(4).SequenceEqual(new char[] {'M', 'T', 'r', 'k'})) // Track start
                        return;
                    
                    long end = _read.BaseStream.Position + BitConverter.ToInt32(_read.ReadBytes(4).Reverse().ToArray()); // Track length
                    List<Timer> timers = new List<Timer>();
                    List<int> timecodes = new List<int>();
                    List<Signal> signals = new List<Signal>();
                    int time = 0;
                    int check = 0;

                    while (_read.BaseStream.Position < end) {
                        int delta = ReadVariableLength();
                        time += delta;

                        if (delta > 0)
                            check = timers.Count;
                        
                        byte type = _read.ReadByte();
                        Signal n; bool add; int remove;

                        switch (type >> 4) {
                            case 0x8: // Note off
                                n = new Signal(_read.ReadBytes(2)[0], new Color(0));
                                add = true;

                                for (int i = check; i < timers.Count; i++) {
                                    if (signals[i].Index == n.Index) {
                                        add = false;
                                        break;
                                    }
                                }

                                if (add) {
                                    timers.Add(new Timer(_timerexit, timers.Count, Timeout.Infinite, Timeout.Infinite));
                                    timecodes.Add(time);
                                    signals.Add(n);
                                }
                                break;
                            
                            case 0x9: // Note on
                                n = new Signal(_read.ReadByte(), new Color((byte)(_read.ReadByte() >> 1)));
                                add = true; remove = -1;

                                for (int i = check; i < timers.Count; i++) {
                                    if (signals[i].Index == n.Index) {
                                        if (signals[i].Color.Lit) {
                                            add = false;
                                            break;

                                        } else remove = i;
                                    }
                                }

                                if (add) {
                                    if (remove != -1) {
                                        signals.RemoveAt(remove);
                                        timers.RemoveAt(remove);
                                        timecodes.RemoveAt(remove);

                                        for (int i = remove; i < timers.Count; i++)
                                            timers[i] = new Timer(_timerexit, i, Timeout.Infinite, Timeout.Infinite);
                                    }

                                    timers.Add(new Timer(_timerexit, timers.Count, Timeout.Infinite, Timeout.Infinite));
                                    timecodes.Add(time);
                                    signals.Add(n);
                                }
                                break;
                            
                            case 0xA: // Poly Aftertouch
                            case 0xB: // CC
                            case 0xE: // Pitch Wheel
                            case 0x7: // Channel Mode
                                _read.ReadBytes(2);
                                break;
                            
                            case 0xC: // Program Change
                            case 0xD: // Channel Aftertouch
                                _read.ReadByte();
                                break;

                            case 0xF: // System Common
                                DiscardSystem((byte)(type & 0x0F));
                                break;
                        }
                    }

                    if (_read.BaseStream.Position == end) {
                        _path = value;
                        _timers = timers;
                        _timecodes = timecodes;
                        _signals = signals;
                    }
                }
            }
        }

        public override Device Clone() => new Lightweight(_path);

        public Lightweight(string path = null): base(DeviceIdentifier) {
            _timerexit = new TimerCallback(Tick);

            if (path != null) Path = path;
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(int)) {
                Signal n = _signals[(int)info].Clone();
                
                n.Index = Conversion.DRtoXY[n.Index];

                MIDIExit?.Invoke(n);
            }
        }

        public override void MIDIEnter(Signal n) {
            if (n.Color.Lit)
                for (int i = 0; i < _timers.Count; i++)
                    _timers[i].Change(Decimal.ToInt32(_timecodes[i] * 2500 / Program.Project.BPM), Timeout.Infinite);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Lightweight(
                data["path"].ToString()
            );
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("path");
                        writer.WriteValue(_path);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}