using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Selection;

namespace Apollo.Undo {
    public class UndoEntry {
        public readonly string Description;

        UndoEntry Post;

        public void AddPost(UndoEntry entry) {
            if (Post == null) Post = entry;
            else Post.AddPost(entry);
        }

        public void Undo() {
            OnUndo();
            Post?.Undo();
        }

        public void Redo() {
            OnRedo();
            Post?.Redo();
        }

        public void Dispose() {
            OnDispose();
            Post?.Dispose();
        }

        protected virtual void OnUndo() {}
        protected virtual void OnRedo() {}
        protected virtual void OnDispose() {}

        public UndoEntry(string desc) => Description = desc;

        protected UndoEntry(BinaryReader reader, int version) {
            Description = reader.ReadString();

            if (reader.ReadBoolean()) // has Post
                Post = DecodeEntry(reader, version);
        }

        public virtual void Encode(BinaryWriter writer) {
            writer.Write(Description);

            writer.Write(Post != null);
            Post?.Encode(writer);
        }

        public static UndoEntry DecodeEntry(BinaryReader reader, int version)
            => (UndoEntry)Activator.CreateInstance(
                UndoBinary.DecodeID(reader),
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new object[] { reader, version },
                null
            );
    }

    public abstract class SimpleUndoEntry<I>: UndoEntry {
        I u, r;

        protected override void OnUndo() => Action(u);
        protected override void OnRedo() => Action(r);

        protected virtual void Action(I element) {}

        protected override void OnDispose() => OnDispose(u, r);
        protected virtual void OnDispose(I undo, I redo) {}

        public SimpleUndoEntry(string desc, I undo, I redo): base(desc) {
            this.u = undo;
            this.r = redo;
        }

        protected SimpleUndoEntry(BinaryReader reader, int version)
        : base(reader, version) {
            u = Decoder.DecodeAnything<I>(reader, version);
            r = Decoder.DecodeAnything<I>(reader, version);
        }

        public override void Encode(BinaryWriter writer) {
            base.Encode(writer);

            Encoder.EncodeAnything(writer, u);
            Encoder.EncodeAnything(writer, r);
        }
    }

    public abstract class SimpleIndexUndoEntry<I>: SimpleUndoEntry<I> {
        int index;

        protected override void Action(I element) => Action(index, element);
        protected virtual void Action(int index, I element) {}

        protected override void OnDispose(I undo, I redo) => OnDispose(index, undo, redo);
        protected virtual void OnDispose(int index, I undo, I redo) {}

        public SimpleIndexUndoEntry(string desc, int index, I undo, I redo)
        : base(desc, undo, redo) => this.index = index;

        protected SimpleIndexUndoEntry(BinaryReader reader, int version)
        : base(reader, version) => index = reader.ReadInt32();

        public override void Encode(BinaryWriter writer) {
            base.Encode(writer);

            writer.Write(index);
        }
    }

    public abstract class PathUndoEntry<T>: UndoEntry {
        protected List<Path<T>> Paths;
        protected T[] Items => Paths.Select(i => i.Resolve()).ToArray();

        protected override void OnUndo() => UndoPath(Items);
        protected virtual void UndoPath(params T[] items) {}

        protected override void OnRedo() => RedoPath(Items);
        protected virtual void RedoPath(params T[] items) {}

        public PathUndoEntry(string desc, params T[] children)
        : base(desc) => Paths = children.Select(i => new Path<T>(i)).ToList();

        protected PathUndoEntry(BinaryReader reader, int version)
        : base(reader, version) => Paths = Enumerable.Range(0, reader.ReadInt32()).Select(i => new Path<T>(reader, version)).ToList();

        public override void Encode(BinaryWriter writer) {
            base.Encode(writer);

            writer.Write(Paths.Count);
            for (int i = 0; i < Paths.Count; i++)
                Paths[i].Encode(writer);
        }
    }

    public abstract class SinglePathUndoEntry<T>: PathUndoEntry<T> {
        protected override void UndoPath(params T[] items) => Undo(items[0]);
        protected virtual void Undo(T item) {}

        protected override void RedoPath(params T[] items) => Redo(items[0]);
        protected virtual void Redo(T item) {}

        public SinglePathUndoEntry(string desc, T item): base(desc, item) {}

        protected SinglePathUndoEntry(BinaryReader reader, int version)
        : base(reader, version) {}
    }

    public abstract class SimplePathUndoEntry<T, I>: SinglePathUndoEntry<T> {
        I u, r;

        protected override void Undo(T item) => Action(item, u);
        protected override void Redo(T item) => Action(item, r);

        protected virtual void Action(T item, I element) {}

        protected override void OnDispose() => OnDispose(u, r);
        protected virtual void OnDispose(I undo, I redo) {}

        public SimplePathUndoEntry(string desc, T child, I undo, I redo): base(desc, child) {
            this.u = undo;
            this.r = redo;
        }

        protected SimplePathUndoEntry(BinaryReader reader, int version)
        : base(reader, version) {
            u = Decoder.DecodeAnything<I>(reader, version);
            r = Decoder.DecodeAnything<I>(reader, version);
        }

        public override void Encode(BinaryWriter writer) {
            base.Encode(writer);

            Encoder.EncodeAnything(writer, u);
            Encoder.EncodeAnything(writer, r);
        }
    }

    public abstract class EnumSimplePathUndoEntry<T, I>: SimplePathUndoEntry<T, I> where I: Enum {
        public EnumSimplePathUndoEntry(string desc, T child, I undo, I redo, IEnumerable textSource)
        : base($"{desc} Changed to {textSource.OfType<ComboBoxItem>().ElementAt((int)(object)redo).Content}", child, undo, redo) {}
    }

    public abstract class SymmetricPathUndoEntry<T>: SinglePathUndoEntry<T> {
        protected override void Undo(T item) => Action(item);
        protected override void Redo(T item) => Action(item);

        protected virtual void Action(T item) {}

        public SymmetricPathUndoEntry(string desc, T child): base(desc, child) {}

        protected SymmetricPathUndoEntry(BinaryReader reader, int version)
        : base(reader, version) {}
    }

    public abstract class SimpleIndexPathUndoEntry<T, I>: SimplePathUndoEntry<T, I> {
        int index;

        protected override void Action(T item, I element) => Action(item, index, element);
        protected virtual void Action(T item, int index, I element) {}

        public SimpleIndexPathUndoEntry(string desc, T child, int index, I undo, I redo)
        : base(desc, child, undo, redo) => this.index = index;
        
        protected SimpleIndexPathUndoEntry(BinaryReader reader, int version)
        : base(reader, version) => index = reader.ReadInt32();

        public override void Encode(BinaryWriter writer) {
            base.Encode(writer);

            writer.Write(index);
        }
    }
}