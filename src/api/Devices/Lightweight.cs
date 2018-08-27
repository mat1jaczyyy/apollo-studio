using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Lightweight: Device {
        private string _path;
        private BinaryReader _read;
        private List<Timer> _timers;
        private List<int> _timecodes;
        private TimerCallback _timerexit;

        private int ReadVariableLength() {
            int ret = 0;
            for (int i = 0; i < 4; i++) {
                byte b = _read.ReadByte();
                ret <<= 7;
                ret += (b & 0x7F);
                if (b >> 7 == 0)
                    return ret;
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
            get {
                return _path;
            }
            set {
                if (!File.Exists(value))
                    return;
                
                _read = new BinaryReader(File.Open(value, FileMode.Open));

                if (!_read.ReadChars(4).SequenceEqual(new char[] {'M', 'T', 'h', 'd'})) // Header identifier
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
                int time = 0;

                while (_read.BaseStream.Position < end) {
                    int varl = ReadVariableLength();
                    time += varl;
                    
                    byte type = _read.ReadByte();
                    switch (type >> 4) {
                        case 0x8: // Note off
                            timers.Add(new Timer(_timerexit, new Signal(_read.ReadByte(), 0), System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite));
                            _read.ReadByte(); // Skip velocity
                            timecodes.Add(time);
                            break;
                        
                        case 0x9: // Note on
                            timers.Add(new Timer(_timerexit, new Signal(_read.ReadByte(), (byte)(_read.ReadByte() >> 1)), System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite));
                            timecodes.Add(time);
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
                    
                    _read.Close();
                    _read.Dispose();
                }
            }
        }        

        public override Device Clone() {
            return new Lightweight();
        }

        public Lightweight() {
            _timerexit = new TimerCallback(Tick);
            _timers = new List<Timer>();
            _path = null;
            this.MIDIExit = null;
        }

        public Lightweight(string path) {
            _timerexit = new TimerCallback(Tick);
            _timers = new List<Timer>();
            Path = path;
            this.MIDIExit = null;
        }

        public Lightweight(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            _timers = new List<Timer>();
            _path = null;
            this.MIDIExit = exit;
        }

        public Lightweight(string path, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            _timers = new List<Timer>();
            Path = path;
            this.MIDIExit = exit;
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(Signal)) {
                Signal n = (Signal)info;
                
                if (this.MIDIExit != null)
                    this.MIDIExit(n);
            }
        }

        public override void MIDIEnter(Signal n) {
            if (n.Red != 0 || n.Green != 0 || n.Blue != 0)
                for (int i = 0; i < _timers.Count; i++)
                    _timers[i].Change(_timecodes[i] * 10, System.Threading.Timeout.Infinite);
        }
    }
}