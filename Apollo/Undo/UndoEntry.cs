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

        public virtual void Undo() {}
        public virtual void Redo() {}
        public virtual void Dispose() {}

        public UndoEntry(string desc) => Description = desc;
    }

    public abstract class SimpleUndoEntry<I>: UndoEntry {
        I u, r;

        public override void Undo() => Action(u);
        public override void Redo() => Action(r);

        protected virtual void Action(I element) {}

        public override void Dispose() => Dispose(u, r);
        protected virtual void Dispose(I undo, I redo) {}

        public SimpleUndoEntry(string desc, I undo, I redo): base(desc) {
            this.u = undo;
            this.r = redo;
        }
    }

    public abstract class SimpleIndexUndoEntry<I>: SimpleUndoEntry<I> {
        int index;

        protected override void Action(I element) => Action(index, element);
        protected virtual void Action(int index, I element) {}

        protected override void Dispose(I undo, I redo) => Dispose(index, undo, redo);
        protected virtual void Dispose(int index, I undo, I redo) {}

        public SimpleIndexUndoEntry(string desc, int index, I undo, I redo)
        : base(desc, undo, redo) => this.index = index;
    }

    public abstract class PathUndoEntry<T>: UndoEntry where T: ISelect {
        protected List<Path<T>> Paths;
        protected T[] Items => Paths.Select(i => i.Resolve()).ToArray();

        public override void Undo() => UndoPath(Items);
        protected virtual void UndoPath(params T[] items) {}

        public override void Redo() => RedoPath(Items);
        protected virtual void RedoPath(params T[] items) {}

        public PathUndoEntry(string desc, params T[] children): base(desc) => Paths = children.Select(i => new Path<T>(i)).ToList();
    }

    public class PathParentUndoEntry<T>: UndoEntry where T: ISelectParent {
        protected List<Path<ISelect>> Paths;
        protected T[] Items => Paths.Select(i => i == null? (T)(ISelectParent)Program.Project : (T)i.Resolve()).ToArray();

        public override void Undo() => UndoPath(Items);
        protected virtual void UndoPath(params T[] items) {}

        public override void Redo() => RedoPath(Items);
        protected virtual void RedoPath(params T[] items) {}

        public PathParentUndoEntry(string desc, params T[] items)
        : base(desc) => Paths = items.Select(i => (i is Project)? null : new Path<ISelect>((ISelect)i)).ToList();
    }

    public abstract class SimplePathUndoEntry<T, I>: PathUndoEntry<T> where T: ISelect {
        I u, r;

        protected override void UndoPath(params T[] items) => Action(items[0], u);
        protected override void RedoPath(params T[] items) => Action(items[0], r);

        protected virtual void Action(T item, I element) {}

        public override void Dispose() => Dispose(u, r);
        protected virtual void Dispose(I undo, I redo) {}

        public SimplePathUndoEntry(string desc, T child, I undo, I redo): base(desc, child) {
            this.u = undo;
            this.r = redo;
        }
    }

    public abstract class EnumSimplePathUndoEntry<T, I>: SimplePathUndoEntry<T, I> where T: ISelect where I: Enum {
        public EnumSimplePathUndoEntry(string desc, T child, I undo, I redo, IEnumerable textSource)
        : base($"{desc} Changed to {textSource.OfType<ComboBoxItem>().ElementAt((int)(object)redo).Content}", child, undo, redo) {}
    }

    public abstract class SymmetricPathUndoEntry<T>: PathUndoEntry<T> where T: ISelect {
        protected override void UndoPath(params T[] items) => Action(items[0]);
        protected override void RedoPath(params T[] items) => Action(items[0]);

        protected virtual void Action(T item) {}

        public SymmetricPathUndoEntry(string desc, T child): base(desc, child) {}
    }

    public abstract class SimpleIndexPathUndoEntry<T, I>: SimplePathUndoEntry<T, I> where T: ISelect {
        int index;

        protected override void Action(T item, I element) => Action(item, index, element);
        protected virtual void Action(T item, int index, I element) {}

        public SimpleIndexPathUndoEntry(string desc, T child, int index, I undo, I redo)
        : base(desc, child, undo, redo) => this.index = index;
    }
}