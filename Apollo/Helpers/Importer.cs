using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.Core;
using Apollo.Structures;

using SkiaSharp;

namespace Apollo.Helpers {
    public class Importer {
        private static int MIDIReadVariableLength(BinaryReader reader) {
            int ret = 0;
            for (int i = 0; i < 4; i++) {
                byte b = reader.ReadByte();
                ret <<= 7;
                ret += (b & 0x7F);
                if (b >> 7 == 0) return ret;
            }
            return ret;
        }

        private static void MIDIDiscardMeta(BinaryReader reader) {
            switch (reader.ReadByte()) {
                case 0x00: // Sequence number
                case 0x59: // Key signature
                    reader.ReadBytes(3);
                    break;

                case 0x01: // Text event
                case 0x02: // Copyright notice
                case 0x03: // Track name
                case 0x04: // Instrument name
                case 0x05: // Lyric
                case 0x06: // Marker
                case 0x07: // Cue Point
                case 0x7F: // Sequencer specific
                    reader.ReadBytes(MIDIReadVariableLength(reader));
                    break;

                case 0x20: // MIDI channel prefix
                case 0x21: // MIDI Port
                    reader.ReadBytes(2);
                    break;
                
                case 0x2F: // End of Track
                    reader.ReadByte();
                    break;
                
                case 0x51: // Tempo
                    reader.ReadBytes(4);
                    break;
                
                case 0x54: // SMPTE Offset
                    reader.ReadBytes(6);
                    break;
                
                case 0x58: // Time signature
                    reader.ReadBytes(5);
                    break;
            }
        }
        
        public static bool FramesFromMIDI(string path, out List<Frame> ret) {
            ret = null;

            if (!File.Exists(path)) return false;
            
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))) {
                if (!reader.ReadChars(4).SequenceEqual(new char[] {'M', 'T', 'h', 'd'}) || // Header Identifier
                    !reader.ReadBytes(4).SequenceEqual(new byte[] {0x00, 0x00, 0x00, 0x06}) || // Header size
                    !reader.ReadBytes(4).SequenceEqual(new byte[] {0x00, 0x00, 0x00, 0x01})) // Single track file
                    return false;
                
                reader.ReadBytes(2); // Skip BPM info (usually 0x00, 0x60 = 120BPM)

                if (!reader.ReadChars(4).SequenceEqual(new char[] {'M', 'T', 'r', 'k'})) // Track start
                    return false;
                
                long end = reader.BaseStream.Position + BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray()); // Track length
                ret = new List<Frame>() {new Frame()};

                while (reader.BaseStream.Position < end) {
                    int delta = MIDIReadVariableLength(reader);
                    if (delta > 0) {
                        ret.Last().Time = new Time(false, null, delta * 2500 / Program.Project.BPM);
                        ret.Add(new Frame());
                    }
                    
                    byte type = reader.ReadByte();

                    switch (type >> 4) {                        
                        case 0x9: // Note on
                            byte index = Converter.DRtoXY(reader.ReadByte());
                            ret.Last().Screen[index] = new Color((byte)(reader.ReadByte() >> 1));
                            break;
                        
                        case 0x7: // Channel Mode
                        case 0x8: // Note off
                        case 0xA: // Poly Aftertouch
                        case 0xB: // CC
                        case 0xE: // Pitch Wheel
                            reader.ReadBytes(2);
                            break;
                        
                        case 0xC: // Program Change
                        case 0xD: // Channel Aftertouch
                            reader.ReadByte();
                            break;

                        case 0xF: // System Common
                            switch ((byte)(type & 0x0F)) {
                                case 0x0: // SysEx Start
                                    reader.ReadBytes(MIDIReadVariableLength(reader));
                                    break;
                                
                                case 0x2: // Song position pointer
                                    reader.ReadBytes(2);
                                    break;

                                case 0x3: // Song select
                                    reader.ReadByte();
                                    break;
                                
                                case 0xF: // Meta
                                    MIDIDiscardMeta(reader);
                                    break;
                            }
                            break;
                    }
                }

                if (ret.Count > 1) ret.RemoveAt(ret.Count - 1);
                return reader.BaseStream.Position == end;
            }
        }

        private static readonly SKImageInfo targetInfo = new SKImageInfo(10, 10);

        public static bool FramesFromImage(string path, out List<Frame> ret) {
            ret = null;
            
            if (!File.Exists(path)) return false;
            
            using (SKCodec codec = SKCodec.Create(path)){
                if (codec == null) return false;

                SKImageInfo info = codec.Info;
                ret = new List<Frame>();
                
                for (int i = 0; i < codec.FrameCount; i++) {
                    SKBitmap frame = new SKBitmap(info);

                    if (codec.GetPixels(info, frame.GetPixels(), new SKCodecOptions(i)) == SKCodecResult.Success) {
                        frame = frame.Resize(targetInfo, SKFilterQuality.High);

                        ret.Add(new Frame(new Time(false, null, codec.FrameInfo[i].Duration)));

                        for (int x = 0; x <= 9; x++) {
                            for (int y = 0; y <= 9; y++) {
                                SKColor color = frame.GetPixel(x, 9 - y);
                                ret[i].Screen[y * 10 + x] = new Color(
                                    (byte)(color.Red >> 2),
                                    (byte)(color.Green >> 2),
                                    (byte)(color.Blue >> 2)
                                );
                            }
                        }

                    } else return false;
                }

                return true;
            }
        }
    }
}