using Apollo.Selection;

namespace Apollo.Undo {
    public abstract class SimplePathUndoEntry<T, I>: PathUndoEntry<T> where T: ISelect {
        I u, r;

        protected override void UndoPath(params T[] items) => Action(items[0], u);
        protected override void RedoPath(params T[] items) => Action(items[0], r);

        protected virtual void Action(T item, I element) {}

        protected override void DisposePath(params T[] items) => Dispose(items[0], u, r);
        protected virtual void Dispose(T item, I undo, I redo) {}

        public SimplePathUndoEntry(string desc, T child, I undo, I redo): base(desc, child) {
            this.u = undo;
            this.r = redo;
        }
    }
}