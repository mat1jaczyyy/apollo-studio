using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Binary {
    public static class Decoder {
        private static bool DecodeHeader(BinaryReader reader) {
            return reader.ReadChars(4).SequenceEqual(new char[] {'A', 'P', 'O', 'L'});
        }

        private static Type DecodeID(BinaryReader reader) {
            return Common.id[(reader.ReadByte())];
        }

        public static dynamic Decode(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                DecodeHeader(reader);
                return Decode(reader, reader.ReadInt32());
            }
        }
        
        private static dynamic Decode(BinaryReader reader, int version) {
            Type t = DecodeID(reader);

            if (t == typeof(Project))
                return new Project(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Track)Decode(reader, version)).ToList()
                );
            
            else if (t == typeof(Track))
                return new Track(
                    (Chain)Decode(reader, version),
                    (Launchpad)Decode(reader, version)
                );
            
            else if (t == typeof(Chain))
                return new Chain(
                    (from i in Enumerable.Range(0, reader.ReadInt32()) select (Device)Decode(reader, version)).ToList()
                );
            
            else if (t == typeof(Device))
                return (Device)Decode(reader, version);
            
            else if (t == typeof(Launchpad)) {
                string name = reader.ReadString();

                foreach (Launchpad lp in MIDI.Devices)
                    if (lp.Name == name) return lp;
                
                return new Launchpad(name);
            }
            
            return null;
        }
    }
}