using Apollo.Selection;

namespace Apollo.Undo {
    public abstract class SymmetricPathUndoEntry<T>: PathUndoEntry<T> where T: ISelect {
        protected override void UndoPath(params T[] items) => Action(items[0]);
        protected override void RedoPath(params T[] items) => Action(items[0]);

        protected virtual void Action(T item) {}

        public SymmetricPathUndoEntry(string desc, T child): base(desc, child) {}
    }
}