using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;

namespace Apollo.Binary {
    public static class Decoder {
        private static bool DecodeHeader(BinaryReader reader) {
            return reader.ReadChars(4).SequenceEqual(new char[] {'A', 'P', 'O', 'L'});
        }

        private static Type DecodeID(BinaryReader reader) {
            return Common.id[(reader.ReadByte())];
        }

        public static dynamic Decode(Stream input, Type ensure) {
            using (BinaryReader reader = new BinaryReader(input)) {
                DecodeHeader(reader);
                return Decode(reader, reader.ReadInt32(), ensure, true);
            }
        }
        
        private static dynamic Decode(BinaryReader reader, int version, Type ensure = null, bool root = false) {
            Type t = DecodeID(reader);
            if (ensure != null && ensure != t) return null;

            if (t == typeof(Preferences) && root) {
                try {
                    Preferences.AlwaysOnTop = reader.ReadBoolean();
                    Preferences.CenterTrackContents = reader.ReadBoolean();
                    Preferences.AutoCreateKeyFilter = reader.ReadBoolean();
                    Preferences.AutoCreatePageFilter = reader.ReadBoolean();
                    Preferences.FadeSmoothness = reader.ReadDouble();
                    Preferences.CopyPreviousFrame = reader.ReadBoolean();
                    Preferences.EnableGestures = reader.ReadBoolean();

                    ColorHistory.Set(
                        (from i in Enumerable.Range(0, reader.ReadInt32()) select (Color)Decode(reader, version)).ToList()
                    );

                } catch (EndOfStreamException) {
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
            
            else if (t == typeof(Track))
                return new Track(
                    (Chain)Decode(reader, version),
                    (Launchpad)Decode(reader, version),
                    reader.ReadString()
                );
            
            else if (t == typeof(Chain))
                return new Chain(
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Device)Decode(reader, version)).ToList(),
                    reader.ReadString()
                );
            
            else if (t == typeof(Device))
                return Decode(reader, version);
            
            else if (t == typeof(Launchpad)) {
                string name = reader.ReadString();
                
                if (name == "") return null;

                foreach (Launchpad lp in MIDI.Devices)
                    if (lp.Name == name) return lp;
                
                return new Launchpad(name);

            } else if (t == typeof(Group))
                return new Group(
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Chain)Decode(reader, version)).ToList(),
                    reader.ReadBoolean()? (int?)reader.ReadInt32() : null
                );
            
            else if (t == typeof(Copy))
                return new Copy(
                    reader.ReadBoolean(),
                    Decode(reader, version),
                    reader.ReadInt32(),
                    reader.ReadDecimal(),
                    (Copy.CopyType)reader.ReadInt32(),
                    (Copy.GridType)reader.ReadInt32(),
                    reader.ReadBoolean(),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Offset)Decode(reader, version)).ToList()
                );
            
            else if (t == typeof(Delay))
                return new Delay(
                    reader.ReadBoolean(),
                    Decode(reader, version),
                    reader.ReadInt32(),
                    reader.ReadDecimal()
                );
            
            else if (t == typeof(Fade)) {
                int count;
                return new Fade(
                    reader.ReadBoolean(),
                    Decode(reader, version),
                    reader.ReadInt32(),
                    reader.ReadDecimal(),
                    (Fade.PlaybackType)reader.ReadInt32(),
                    (from i in Enumerable.Range(0, count = reader.ReadInt32()) select (Color)Decode(reader, version)).ToList(),
                    (from i in Enumerable.Range(0, count) select reader.ReadDecimal()).ToList()
                );

            } else if (t == typeof(Flip))
                return new Flip(
                    (Flip.FlipType)reader.ReadInt32(),
                    reader.ReadBoolean()
                );
            
            else if (t == typeof(Hold))
                return new Hold(
                    reader.ReadBoolean(),
                    Decode(reader, version),
                    reader.ReadInt32(),
                    reader.ReadDecimal(),
                    reader.ReadBoolean(),
                    reader.ReadBoolean()
                );
            
            else if (t == typeof(KeyFilter))
                return new KeyFilter(
                    (from i in Enumerable.Range(0, 100) select reader.ReadBoolean()).ToArray()
                );

            else if (t == typeof(Layer))
                return new Layer(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Move))
                return new Move(
                    Decode(reader, version),
                    (Move.GridType)reader.ReadInt32(),
                    reader.ReadBoolean()
                );
            
            else if (t == typeof(Multi))
                return new Multi(
                    Decode(reader, version),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Chain)Decode(reader, version)).ToList(),
                    (Multi.MultiType)reader.ReadInt32(),
                    reader.ReadBoolean()? (int?)reader.ReadInt32() : null
                );
            
            else if (t == typeof(Output))
                return new Output(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(PageFilter))
                return new PageFilter(
                    (from i in Enumerable.Range(0, 100) select reader.ReadBoolean()).ToArray()
                );
            
            else if (t == typeof(PageSwitch))
                return new PageSwitch(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Paint))
                return new Paint(
                    Decode(reader, version)
                );
            
            else if (t == typeof(Pattern))
                return new Pattern(
                    reader.ReadDecimal(),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Frame)Decode(reader, version)).ToList(),
                    (Pattern.PlaybackType)reader.ReadInt32(),
                    reader.ReadBoolean()? (int?)reader.ReadInt32() : null,
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Preview))
                return new Preview();
            
            else if (t == typeof(Rotate))
                return new Rotate(
                    (Rotate.RotateType)reader.ReadInt32(),
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
            
            else if (t == typeof(Frame))
                return new Frame(
                    reader.ReadBoolean(),
                    Decode(reader, version),
                    reader.ReadInt32(),
                    (from i in Enumerable.Range(0, 100) select (Color)Decode(reader, version)).ToArray()
                );
            
            else if (t == typeof(Length))
                return new Length(
                    reader.ReadInt32()
                );
            
            else if (t == typeof(Offset))
                return new Offset(
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );

            return null;
        }
    }
}