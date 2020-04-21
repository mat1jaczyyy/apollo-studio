using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;

using Apollo.Core;
using Apollo.Elements;
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
    }

    public abstract class SimpleIndexUndoEntry<I>: SimpleUndoEntry<I> {
        int index;

        protected override void Action(I element) => Action(index, element);
        protected virtual void Action(int index, I element) {}

        protected override void OnDispose(I undo, I redo) => OnDispose(index, undo, redo);
        protected virtual void OnDispose(int index, I undo, I redo) {}

        public SimpleIndexUndoEntry(string desc, int index, I undo, I redo)
        : base(desc, undo, redo) => this.index = index;
    }

    public abstract class PathUndoEntry<T>: UndoEntry {
        protected List<Path<T>> Paths;
        protected T[] Items => Paths.Select(i => i.Resolve()).ToArray();

        protected override void OnUndo() => UndoPath(Items);
        protected virtual void UndoPath(params T[] items) {}

        protected override void OnRedo() => RedoPath(Items);
        protected virtual void RedoPath(params T[] items) {}

        public PathUndoEntry(string desc, params T[] children): base(desc) => Paths = children.Select(i => new Path<T>(i)).ToList();
    }

    public abstract class SinglePathUndoEntry<T>: PathUndoEntry<T> {
        protected override void UndoPath(params T[] items) => Undo(items[0]);
        protected virtual void Undo(T item) {}

        protected override void RedoPath(params T[] items) => Redo(items[0]);
        protected virtual void Redo(T item) {}

        public SinglePathUndoEntry(string desc, T item): base(desc, item) {}
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
    }

    public abstract class SimpleIndexPathUndoEntry<T, I>: SimplePathUndoEntry<T, I> {
        int index;

        protected override void Action(T item, I element) => Action(item, index, element);
        protected virtual void Action(T item, int index, I element) {}

        public SimpleIndexPathUndoEntry(string desc, T child, int index, I undo, I redo)
        : base(desc, child, undo, redo) => this.index = index;
    }
}