using Apollo.Selection;

namespace Apollo.Undo {
    public abstract class SimpleIndexUndoEntry<T, I>: SimpleUndoEntry<T, I> where T: ISelect {
        int index;

        protected override void Action(T item, I element) => Action(item, index, element);
        protected virtual void Action(T item, int index, I element) {}

        protected override void Dispose(T item, I undo, I redo) => Dispose(item, index, undo, redo);
        protected virtual void Dispose(T item, int index, I undo, I redo) {}

        public SimpleIndexUndoEntry(string desc, T child, int index, I undo, I redo)
        : base(desc, child, undo, redo) => this.index = index;
    }
}