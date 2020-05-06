using System;
using System.IO;

using Apollo.Devices;

namespace Apollo.Undo {
    public static class UndoBinary {
        public static Type DecodeID(BinaryReader reader) => id[reader.ReadUInt16()];
        public static void EncodeID(BinaryWriter writer, Type type) => writer.Write((ushort)Array.IndexOf(id, type));

        public static readonly Type[] id = new Type[] {
            typeof(UndoEntry),
            typeof(Copy.OffsetRelativeUndoEntry)
        };
    }
}