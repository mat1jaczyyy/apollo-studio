using System;
using System.Collections;
using System.Linq;

using Avalonia.Controls;

using Apollo.Selection;

namespace Apollo.Undo {
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
}