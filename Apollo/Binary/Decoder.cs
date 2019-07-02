using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Interfaces;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Binary {
    public static class Decoder {
        static bool DecodeHeader(BinaryReader reader) => reader.ReadChars(4).SequenceEqual(new char[] {'A', 'P', 'O', 'L'});

        static Type DecodeID(BinaryReader reader) => Common.id[(reader.ReadByte())];

        public static async Task<dynamic> Decode(Stream input, Type ensure) {
            using (BinaryReader reader = new BinaryReader(input)) {
                if (!DecodeHeader(reader)) return new InvalidDataException();

                int version = reader.ReadInt32();

                if (version > Common.version && await MessageWindow.Create(
                    "The content you're attempting to read was created with a newer version of\n" +
                    "Apollo Studio, which might result in it not loading correctly.\n\n" +
                    "Are you sure you want to proceed?",
                    new string[] { "Yes", "No" }, null
                ) == "No") return new InvalidDataException();

                return Decode(reader, version, ensure, true);
            }
        }

        public static dynamic DecodeBlock(Stream input, Type ensure) {
            using (BinaryReader reader = new BinaryReader(input)) {
                if (!DecodeHeader(reader)) return new InvalidDataException();

                int version = reader.ReadInt32();

                if (version > Common.version) return new InvalidDataException();

                return Decode(reader, version, ensure, true);
            }
        }
        
        static dynamic Decode(BinaryReader reader, int version, Type ensure = null, bool root = false) {
            Type t = DecodeID(reader);
            if (ensure != null && ensure != t) return new InvalidDataException();

            if (t == typeof(Preferences) && root) {
                try {
                    Preferences.AlwaysOnTop = reader.ReadBoolean();
                    Preferences.CenterTrackContents = reader.ReadBoolean();

                    if (version >= 9) {
                        Preferences.LaunchpadStyle = (LaunchpadStyles)reader.ReadInt32();
                    }

                    if (version >= 14) {
                        Preferences.LaunchpadGridRotation = reader.ReadInt32() > 0;
                    }

                    Preferences.AutoCreateKeyFilter = reader.ReadBoolean();
                    Preferences.AutoCreatePageFilter = reader.ReadBoolean();

                    if (version >= 11) {
                        Preferences.AutoCreatePattern = reader.ReadBoolean();
                    }

                    Preferences.FadeSmoothness = reader.ReadDouble();
                    Preferences.CopyPreviousFrame = reader.ReadBoolean();

                    if (version >= 7) {
                        Preferences.CaptureLaunchpad = reader.ReadBoolean();
                    }
                    
                    Preferences.EnableGestures = reader.ReadBoolean();

                    if (version >= 7) {
                        Preferences.PaletteName = reader.ReadString();
                        Preferences.CustomPalette = new Palette((from i in Enumerable.Range(0, 128) select (Color)Decode(reader, version)).ToArray());
                        Preferences.ImportPalette = (Palettes)reader.ReadInt32();

                        Preferences.Theme = (Themes)reader.ReadInt32();
                    }

                    if (version >= 10) {
                        Preferences.Backup = reader.ReadBoolean();
                        Preferences.Autosave = reader.ReadBoolean();
                    }

                    if (version >= 12) {
                        Preferences.UndoLimit = reader.ReadBoolean();
                    }

                    if (version <= 0) {
                        Preferences.DiscordPresence = true;
                        reader.ReadBoolean();
                    } else {
                        Preferences.DiscordPresence = reader.ReadBoolean();
                    }
                    
                    Preferences.DiscordFilename = reader.ReadBoolean();

                    ColorHistory.Set(
                        (from i in Enumerable.Range(0, reader.ReadInt32()) select (Color)Decode(reader, version)).ToList()
                    );

                    if (version >= 2)
                        MIDI.Devices = (from i in Enumerable.Range(0, reader.ReadInt32()) select (Launchpad)Decode(reader, version)).ToList();

                    if (version >= 15) {
                        Preferences.Recents = (from i in Enumerable.Range(0, reader.ReadInt32()) select reader.ReadString()).ToList();
                        Preferences.CrashName = reader.ReadString();
                        Preferences.CrashPath = reader.ReadString();
                    }
                    
                    return null;

                } catch {
                    Program.Log("Error reading Preferences");
                    return null;
                }

            } else if (t == typeof(Copyable)) {
                return new Copyable() {
                    Contents = (from i in Enumerable.Range(0, reader.ReadInt32()) select (ISelect)Decode(reader, version)).ToList()
                };

            } else if (t == typeof(Project))
                return new Project(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Track)Decode(reader, version)).ToList()
                );
            
            else if (t == typeof(Track)) {
                Chain chain = (Chain)Decode(reader, version);
                Launchpad lp = (Launchpad)Decode(reader, version);
                string name = reader.ReadString();

                bool enabled = true;
                if (version >= 8) {
                    enabled = reader.ReadBoolean();
                }

                return new Track(chain, lp, name) {
                    Enabled = enabled
                };

            } else if (t == typeof(Chain)) {
                List<Device> devices = (from i in Enumerable.Range(0, reader.ReadInt32()) select (Device)Decode(reader, version)).ToList();
                string name = reader.ReadString();

                bool enabled = true;
                if (version >= 6) {
                    enabled = reader.ReadBoolean();
                }

                return new Chain(devices, name) {
                    Enabled = enabled
                };

            } else if (t == typeof(Device)) {
                bool collapsed = false;
                if (version >= 5) {
                    collapsed = reader.ReadBoolean();
                }

                bool enabled = true;
                if (version >= 5) {
                    enabled = reader.ReadBoolean();
                }

                Device ret = (Device)Decode(reader, version);
                ret.Collapsed = collapsed;
                ret.Enabled = enabled;

                return ret;
            
            } else if (t == typeof(Launchpad)) {
                string name = reader.ReadString();
                if (name == "") return MIDI.NoOutput;

                InputType format = InputType.DrumRack;
                if (version >= 2) format = (InputType)reader.ReadInt32();

                RotationType rotation = RotationType.D0;
                if (version >= 9) rotation = (RotationType)reader.ReadInt32();

                foreach (Launchpad lp in MIDI.Devices)
                    if (lp.Name == name) {
                        lp.InputFormat = format;
                        lp.Rotation = rotation;
                        return lp;
                    }
                
                return new Launchpad(name, format, rotation);

            } else if (t == typeof(Group))
                return new Group(
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Chain)Decode(reader, version)).ToList(),
                    reader.ReadBoolean()? (int?)reader.ReadInt32() : null
                );

            else if (t == typeof(Choke))
                return new Choke(
                    reader.ReadInt32(),
                    (Chain)Decode(reader, version)
                );

            else if (t == typeof(Copy)) {
                Time time;
                if (version <= 2) {
                    time = new Time(
                        reader.ReadBoolean(),
                        Decode(reader, version),
                        reader.ReadInt32()
                    );
                } else {
                    time = Decode(reader, version);
                }

                double gate;
                if (version <= 13) {
                    gate = (double)reader.ReadDecimal();
                } else {
                    gate = reader.ReadDouble();
                }

                return new Copy(
                    time,
                    gate,
                    (CopyType)reader.ReadInt32(),
                    (GridType)reader.ReadInt32(),
                    reader.ReadBoolean(),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Offset)Decode(reader, version)).ToList()
                );
            
            } else if (t == typeof(Delay)) {
                Time time;
                if (version <= 2) {
                    time = new Time(
                        reader.ReadBoolean(),
                        Decode(reader, version),
                        reader.ReadInt32()
                    );
                } else {
                    time = Decode(reader, version);
                }

                double gate;
                if (version <= 13) {
                    gate = (double)reader.ReadDecimal();
                } else {
                    gate = reader.ReadDouble();
                }

                return new Delay(time, gate);
            
            } else if (t == typeof(Fade)) {
                Time time;
                if (version <= 2) {
                    time = new Time(
                        reader.ReadBoolean(),
                        Decode(reader, version),
                        reader.ReadInt32()
                    );
                } else {
                    time = Decode(reader, version);
                }

                double gate;
                if (version <= 13) {
                    gate = (double)reader.ReadDecimal();
                } else {
                    gate = reader.ReadDouble();
                }

                FadePlaybackType playmode = (FadePlaybackType)reader.ReadInt32();

                int count;
                List<Color> colors = (from i in Enumerable.Range(0, count = reader.ReadInt32()) select (Color)Decode(reader, version)).ToList();
                List<double> positions = (from i in Enumerable.Range(0, count) select (version <= 13)? (double)reader.ReadDecimal() : reader.ReadDouble()).ToList();

                return new Fade(time, gate, playmode, colors, positions);

            } else if (t == typeof(Flip))
                return new Flip(
                    (FlipType)reader.ReadInt32(),
                    reader.ReadBoolean()
                );
            
            else if (t == typeof(Hold)) {
                Time time;
                if (version <= 2) {
                    time = new Time(
                        reader.ReadBoolean(),
                        Decode(reader, version),
                        reader.ReadInt32()
                    );
                } else {
                    time = Decode(reader, version);
                }

                double gate;
                if (version <= 13) {
                    gate = (double)reader.ReadDecimal();
                } else {
                    gate = reader.ReadDouble();
                }

                return new Hold(
                    time,
                    gate,
                    reader.ReadBoolean(),
                    reader.ReadBoolean()
                );
            
            } else if (t == typeof(KeyFilter))
                return new KeyFilter(
                    (from i in Enumerable.Range(0, 100) select reader.ReadBoolean()).ToArray()
                );

            else if (t == typeof(Layer)) {
                int target = reader.ReadInt32();

                BlendingType blending = BlendingType.Normal;
                if (version >= 5) {
                    if (version == 5) {
                        blending = (BlendingType)reader.ReadInt32();
                        if ((int)blending == 2) blending = BlendingType.Mask;

                    } else {
                        blending = (BlendingType)reader.ReadInt32();
                    }
                }

                return new Layer(
                    target,
                    blending
                );

            } else if (t == typeof(Move))
                return new Move(
                    Decode(reader, version),
                    (GridType)reader.ReadInt32(),
                    reader.ReadBoolean()
                );
            
            else if (t == typeof(Multi))
                return new Multi(
                    Decode(reader, version),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Chain)Decode(reader, version)).ToList(),
                    reader.ReadBoolean()? (int?)reader.ReadInt32() : null,
                    (MultiType)reader.ReadInt32()
                );
            
            else if (t == typeof(Output))
                return new Output(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(PageFilter))
                return new PageFilter(
                    (from i in Enumerable.Range(0, 100) select reader.ReadBoolean()).ToArray()
                );
            
            else if (t == typeof(Switch))
                return new Switch(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Paint))
                return new Paint(
                    Decode(reader, version)
                );
            
            else if (t == typeof(Pattern)) {
                int repeats = 1;
                if (version >= 11) {
                    repeats = reader.ReadInt32();
                }

                double gate;
                if (version <= 13) {
                    gate = (double)reader.ReadDecimal();
                } else {
                    gate = reader.ReadDouble();
                }

                List<Frame> frames = (from i in Enumerable.Range(0, reader.ReadInt32()) select (Frame)Decode(reader, version)).ToList();
                PlaybackType mode = (PlaybackType)reader.ReadInt32();

                bool chokeenabled = false;
                int choke = 8;

                if (version <= 10) {
                    chokeenabled = reader.ReadBoolean();

                    if (version <= 0) {
                        if (chokeenabled)
                            choke = reader.ReadInt32();
                    } else {
                        choke = reader.ReadInt32();
                    }
                }

                bool infinite = false;
                if (version >= 4) {
                    infinite = reader.ReadBoolean();
                }

                int? rootkey = null;
                if (version >= 12) {
                    rootkey = reader.ReadBoolean()? (int?)reader.ReadInt32() : null;
                }

                bool wrap = false;
                if (version >= 13) {
                    wrap = reader.ReadBoolean();
                }

                int expanded = reader.ReadInt32();

                Pattern ret = new Pattern(repeats, gate, frames, mode, infinite, rootkey, wrap, expanded);

                if (chokeenabled) {
                    return new Choke(choke, new Chain(new List<Device>() {ret}));
                }

                return ret;
            
            } else if (t == typeof(Preview))
                return new Preview();
            
            else if (t == typeof(Rotate))
                return new Rotate(
                    (RotateType)reader.ReadInt32(),
                    reader.ReadBoolean()
                );
            
            else if (t == typeof(Tone))
                return new Tone(
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble()
                );
            
            else if (t == typeof(Color))
                return new Color(
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte()
                );
            
            else if (t == typeof(Frame)) {
                Time time;
                if (version <= 2) {
                    time = new Time(
                        reader.ReadBoolean(),
                        Decode(reader, version),
                        reader.ReadInt32()
                    );
                } else {
                    time = Decode(reader, version);
                }

                return new Frame(
                    time,
                    (from i in Enumerable.Range(0, 100) select (Color)Decode(reader, version)).ToArray()
                );
            
            } else if (t == typeof(Length))
                return new Length(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Offset))
                return new Offset(
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Time))
                return new Time(
                    reader.ReadBoolean(),
                    Decode(reader, version),
                    reader.ReadInt32()
                );

            return new InvalidDataException();
        }
    }
}