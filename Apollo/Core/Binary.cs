using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Core {
    public static class Binary {
        private const int version = 0;

        public static readonly Type[] id = new Type[] {
            typeof(Project),
            typeof(Track),
            typeof(Chain),
            typeof(Device),
            typeof(Launchpad),

            typeof(Group),
            typeof(Copy),
            typeof(Delay),
            typeof(Fade),
            typeof(Flip),
            typeof(Hold),
            typeof(KeyFilter),
            typeof(Layer),
            typeof(Move),
            typeof(Multi),
            typeof(Output),
            typeof(PageFilter),
            typeof(PageSwitch),
            typeof(Paint),
            typeof(Pattern),
            typeof(Preview),
            typeof(Rotate),
            typeof(Tone),

            typeof(Color),
            typeof(Frame),
            typeof(Length),
            typeof(Offset)
        };

        public static MemoryStream Encode(object o) {
            MemoryStream output = new MemoryStream();

            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write(new char[] {'A', 'P', 'O', 'L'});
                writer.Write(version);

                Encode(writer, (dynamic)o);
            }

            return output;
        }

        private static void EncodeID(BinaryWriter writer, Type type) => writer.Write((byte)Array.IndexOf(id, type));

        private static void Encode(BinaryWriter writer, Project o) {
            EncodeID(writer, typeof(Project));

            writer.Write(o.BPM);
            writer.Write(o.Tracks.Count);

            for (int i = 0; i < o.Tracks.Count; i++)
                Encode(writer, o.Tracks[i]);
        }

        private static void Encode(BinaryWriter writer, Track o) {
            EncodeID(writer, typeof(Track));

            Encode(writer, o.Chain);
            Encode(writer, o.Launchpad);
        }

        private static void Encode(BinaryWriter writer, Chain o) {
            EncodeID(writer, typeof(Chain));

            writer.Write(o.Count);

            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
        }

        private static void Encode(BinaryWriter writer, Device o) {
            EncodeID(writer, typeof(Device));

            Encode(writer, (dynamic)o);
        }

        private static void Encode(BinaryWriter writer, Launchpad o) {
            EncodeID(writer, typeof(Launchpad));

            writer.Write(o.Name);
        }
    }
}