using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Apollo.Core;
using Apollo.Structures;

using SkiaSharp;

namespace Apollo.Helpers {
    public class Importer {
        public static Palette Palette = Palette.NovationPalette;

        static int MIDIReadVariableLength(BinaryReader reader) {
            int ret = 0;
            for (int i = 0; i < 4; i++) {
                byte b = reader.ReadByte();
                ret <<= 7;
                ret += (b & 0x7F);
                if (b >> 7 == 0) return ret;
            }
            return ret;
        }

        static void MIDIDiscardMeta(BinaryReader reader) {
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
        
        public static bool FramesFromMIDI(string path, out List<Frame> ret, CancellationToken? ct = null, Action<int, int> progress = null) {
            ret = null;

            if (!File.Exists(path)) return false;
            
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))) {
                
                if (!reader.ReadBytes(4).Select(i => (char)i).SequenceEqual(new char[] {'M', 'T', 'h', 'd'}) || // Header Identifier
                    !reader.ReadBytes(4).SequenceEqual(new byte[] {0x00, 0x00, 0x00, 0x06}) || // Header size
                    !reader.ReadBytes(4).SequenceEqual(new byte[] {0x00, 0x00, 0x00, 0x01})) // Single track file
                    return false;
                
                double beatsize = (reader.ReadByte() << 8) + reader.ReadByte();

                if (!reader.ReadBytes(4).Select(i => (char)i).SequenceEqual(new char[] {'M', 'T', 'r', 'k'})) // Track start
                    return false;
                
                long end = reader.BaseStream.Position + BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray()); // Track length
                ret = new List<Frame>() {new Frame()};

                progress?.Invoke((int)reader.BaseStream.Position, (int)end);

                double totalTime = 0;

                while (reader.BaseStream.Position < end) {
                    if (ct?.IsCancellationRequested == true) return false;

                    int delta = MIDIReadVariableLength(reader);
                    
                    double prev = totalTime;
                    totalTime += delta * 60000 / beatsize / Program.Project.BPM;

                    if (delta > 0) {
                        ret.Last().Time = new Time(false, free: (int)(Math.Round(totalTime) - Math.Round(prev)));
                        ret.Add(ret.Last().Clone());
                    }
                    
                    byte type = reader.ReadByte();

                    switch (type >> 4) {
                        case 0x8: // Note off
                            byte index = Converter.DRtoXY(reader.ReadBytes(2)[0]);
                            ret.Last().Screen[index] = Palette.GetColor(0);
                            break;

                        case 0x9: // Note on
                            index = Converter.DRtoXY(reader.ReadByte());
                            ret.Last().Screen[index] = Palette.GetColor(reader.ReadByte());
                            break;
                        
                        case 0x7: // Channel Mode
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

                    if (delta > 0)
                        progress?.Invoke((int)reader.BaseStream.Position, (int)end);
                }

                if (ret.Count > 1) ret.RemoveAt(ret.Count - 1);
                return reader.BaseStream.Position == end;
            }
        }

        static readonly SKImageInfo targetInfo = new SKImageInfo(10, 10);

        static bool DecodeImageFrame(SKCodec codec, SKBitmap bitmap, out Frame decoded, int index = -1) {
            decoded = null;

            if ((
                (index < 0)
                    ? codec.GetPixels(codec.Info, bitmap.GetPixels())
                    : codec.GetPixels(codec.Info, bitmap.GetPixels(), new SKCodecOptions(index, index - 1))
                ) == SKCodecResult.Success) {

                SKBitmap resized = bitmap.Resize(targetInfo, SKFilterQuality.High);

                decoded = new Frame(new Time(
                    false,
                    free: (index < 0)
                        ? 1000
                        : codec.FrameInfo[index].Duration
                ));

                for (int x = 0; x <= 9; x++) {
                    for (int y = 0; y <= 9; y++) {
                        SKColor color = resized.GetPixel(x, 9 - y);
                        decoded.Screen[y * 10 + x] = new Color(
                            (byte)(color.Red >> 2),
                            (byte)(color.Green >> 2),
                            (byte)(color.Blue >> 2)
                        );
                    }
                }

                return true;

            } else return false;
        }

        public static bool FramesFromImage(string path, out List<Frame> ret, CancellationToken? ct = null, Action<int, int> progress = null) {
            ret = null;
            
            if (!File.Exists(path)) return false;
            
            using (SKCodec codec = SKCodec.Create(path)){
                if (codec == null) return false;

                SKBitmap bitmap = new SKBitmap(codec.Info);
                ret = new List<Frame>();
                
                if (codec.FrameCount > 0)
                    for (int i = 0; i < codec.FrameCount; i++) {
                        if (ct?.IsCancellationRequested != true && DecodeImageFrame(codec, bitmap, out Frame frame, i)) {
                            ret.Add(frame);
                            progress?.Invoke(i + 1, codec.FrameCount);

                        } else return false;
                    }

                else if (DecodeImageFrame(codec, bitmap, out Frame frame)) {
                    ret.Add(frame);
                    progress?.Invoke(1, 1);
                
                } else return false;

                return true;
            }
        }
    }
}