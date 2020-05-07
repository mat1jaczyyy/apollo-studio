using System;
using System.IO;

using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Undo {
    public static class UndoBinary {
        public static Type DecodeID(BinaryReader reader) { // todo crashes a lot here, store id for debug watch
            ushort i = reader.ReadUInt16();  // note getting a 65535 here means you forgot to add undoentry to `id` array
            return id[i];
        }
        public static void EncodeID(BinaryWriter writer, Type type) => writer.Write((ushort)Array.IndexOf(id, type));

        public static readonly Type[] id = new Type[] { // only non-abstract undoentries go here
            typeof(UndoEntry),
            typeof(Copy.OffsetRelativeUndoEntry),
            typeof(Project.AuthorChangedUndoEntry),
            typeof(Project.BPMChangedUndoEntry)
        };
    }
}