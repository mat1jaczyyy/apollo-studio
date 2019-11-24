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
                if (!DecodeHeader(reader)) throw new InvalidDataException();

                int version = reader.ReadInt32();

                if (version > Common.version && await MessageWindow.Create(
                    "The content you're attempting to read was created with a newer version of\n" +
                    "Apollo Studio, which might result in it not loading correctly.\n\n" +
                    "Are you sure you want to proceed?",
                    new string[] { "Yes", "No" }, null
                ) == "No") throw new InvalidDataException();

                return Decode(reader, version, ensure, true);
            }
        }

        public static dynamic DecodeBlock(Stream input, Type ensure) {
            using (BinaryReader reader = new BinaryReader(input)) {
                if (!DecodeHeader(reader)) throw new InvalidDataException();

                int version = reader.ReadInt32();

                if (version > Common.version) throw new InvalidDataException();

                return Decode(reader, version, ensure, true);
            }
        }
        
        static dynamic Decode(BinaryReader reader, int version, Type ensure = null, bool root = false) {
            Type t = DecodeID(reader);
            if (ensure != null && ensure != t) throw new InvalidDataException();

            if (t == typeof(Preferences) && root) {
                Preferences.AlwaysOnTop = reader.ReadBoolean();
                Preferences.CenterTrackContents = reader.ReadBoolean();

                if (version >= 24) {
                    Preferences.ChainSignalIndicators = reader.ReadBoolean();
                    Preferences.DeviceSignalIndicators = reader.ReadBoolean();
                } else if (version >= 23) {
                    Preferences.ChainSignalIndicators = Preferences.DeviceSignalIndicators = reader.ReadBoolean();
                }

                if (version >= 9) {
                    Preferences.LaunchpadStyle = (LaunchpadStyles)reader.ReadInt32();
                }

                if (version >= 14) {
                    Preferences.LaunchpadGridRotation = reader.ReadInt32() > 0;
                }

                if (version >= 24) {
                    Preferences.LaunchpadModel = (LaunchpadModels)reader.ReadInt32();
                }

                Preferences.AutoCreateKeyFilter = reader.ReadBoolean();
                Preferences.AutoCreateMacroFilter = reader.ReadBoolean();

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

                    Preferences.Theme = (ThemeType)reader.ReadInt32();
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
                }

                if (15 <= version && version <= 22) {
                    reader.ReadString();
                    reader.ReadString();
                }

                if (version >= 23) {
                    Preferences.Crashed = reader.ReadBoolean();
                    Preferences.CrashPath = reader.ReadString();
                }

                if (version >= 16)
                    Preferences.CheckForUpdates = reader.ReadBoolean();

                if (version >= 17)
                    Preferences.BaseTime = reader.ReadInt64();
                
                return null;

            } else if (t == typeof(Copyable)) {
                return new Copyable() {
                    Contents = (from i in Enumerable.Range(0, reader.ReadInt32()) select (ISelect)Decode(reader, version)).ToList()
                };

            } else if (t == typeof(Project)) {
                int bpm = reader.ReadInt32();
                int[] macros = (version >= 25)? (from i in Enumerable.Range(0, 4) select reader.ReadInt32()).ToArray() : new int[4] {reader.ReadInt32(), 1, 1, 1};
                List<Track> tracks = (from i in Enumerable.Range(0, reader.ReadInt32()) select (Track)Decode(reader, version)).ToList();

                string author = "";
                long time = 0;
                long started = 0;

                if (version >= 17) {
                    author = reader.ReadString();
                    time = reader.ReadInt64();
                    started = reader.ReadInt64();
                }

                return new Project(bpm, macros, tracks, author, time, started);
            
            } else if (t == typeof(Track)) {
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
                        if (lp.GetType() == typeof(Launchpad)) {
                            lp.InputFormat = format;
                            lp.Rotation = rotation;
                        }
                        return lp;
                    }
                
                Launchpad ret;
                if (name.Contains("Virtual Launchpad ")) ret = new VirtualLaunchpad(name);
                else if (name.Contains("Ableton Connector ")) ret = new AbletonLaunchpad(name);
                else ret = new Launchpad(name, format, rotation);

                MIDI.Devices.Add(ret);

                return ret;

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

            else if (t == typeof(Clear))
                return new Clear(
                    (ClearType)reader.ReadInt32()
                );

            else if (t == typeof(ColorFilter))
                return new ColorFilter(
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble()
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
                List<FadeType> types = (from i in Enumerable.Range(0, count - 1) select (version <= 24) ? FadeType.Linear : (FadeType)reader.ReadInt32()).ToList();

                int? expanded = null;
                if (version >= 23) {
                    expanded = reader.ReadBoolean()? (int?)reader.ReadInt32() : null;
                }

                return new Fade(time, gate, playmode, colors, positions, types, expanded);

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
            
            } else if (t == typeof(KeyFilter)) {
                bool[] filter;
                if (version <= 18) {
                    List<bool> oldFilter = (from i in Enumerable.Range(0, 100) select reader.ReadBoolean()).ToList();
                    oldFilter.Insert(99, false);
                    filter = oldFilter.ToArray();
                } else
                    filter = (from i in Enumerable.Range(0, 101) select reader.ReadBoolean()).ToArray();

                return new KeyFilter(filter);

            } else if (t == typeof(Layer)) {
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

                int range = 200;
                if (version >= 21)
                    range = reader.ReadInt32();

                return new Layer(target, blending, range);

            } else if (t == typeof(LayerFilter))
                return new LayerFilter(
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );

            else if (t == typeof(Move))
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
            
            else if (t == typeof(MacroFilter))
                return new MacroFilter(
                    (version >= 25)? reader.ReadInt32() : 1, (from i in Enumerable.Range(0, 100) select reader.ReadBoolean()).ToArray()
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

                double pinch = 0;
                if (version >= 24) {
                    pinch = reader.ReadDouble();
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

                Pattern ret = new Pattern(repeats, gate, pinch, frames, mode, infinite, rootkey, wrap, expanded);

                if (chokeenabled) {
                    return new Choke(choke, new Chain(new List<Device>() {ret}));
                }

                return ret;
            
            } else if (t == typeof(Preview))
                return new Preview();
            
            else if (t == typeof(Rotate)){
                if(version <= 24){
                    RotateType type = (RotateType)reader.ReadInt32();
                    double angle = 0;
                    
                    switch(type){
                        case RotateType.D90:
                            angle = Math.PI/2;
                            break;
                        case RotateType.D180:
                            angle = Math.PI;
                            break;
                        case RotateType.D270:
                            angle = -Math.PI/2;
                            break;
                    }
                    
                    return new Rotate(
                        angle,
                        reader.ReadBoolean()
                    );
                } else return new Rotate(
                    reader.ReadDouble(),
                    reader.ReadBoolean()
                );
            } else if (t == typeof(Switch)) {
                int target = (version >= 25)? reader.ReadInt32() : 1;
                int value = reader.ReadInt32();

                if (18 <= version && version <= 21 && reader.ReadBoolean())
                    return new Group(new List<Chain>() {
                        new Chain(new List<Device>() {
                            new Switch(1, value), new Clear(ClearType.Multi)
                        }, "Switch Reset")
                    });

                return new Switch(target, value);
            
            } else if (t == typeof(Tone))
                return new Tone(
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble()
                );
            
            else if (t == typeof(Color)) {
                byte red = reader.ReadByte();
                byte green = reader.ReadByte();
                byte blue = reader.ReadByte();

                if (version == 24) {
                    if (red > 0) red = (byte)((red - 1) * 62.0 / 126 + 1);
                    if (green > 0) green = (byte)((green - 1) * 62.0 / 126 + 1);
                    if (blue > 0) blue = (byte)((blue - 1) * 62.0 / 126 + 1);
                }

                return new Color(red, green, blue);
            
            } else if (t == typeof(Frame)) {
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

                Color[] screen;
                if (version <= 19) {
                    List<Color> oldScreen = (from i in Enumerable.Range(0, 100) select (Color)Decode(reader, version)).ToList();
                    oldScreen.Insert(99, new Color(0));
                    screen = oldScreen.ToArray();
                } else
                    screen = (from i in Enumerable.Range(0, 101) select (Color)Decode(reader, version)).ToArray();

                return new Frame(time, screen);
            
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

            throw new InvalidDataException();
        }
    }
}