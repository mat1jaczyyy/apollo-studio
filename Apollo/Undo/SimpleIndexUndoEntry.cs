namespace Apollo.Undo {
    public abstract class SimpleIndexUndoEntry<I>: SimpleUndoEntry<I> {
        int index;

        protected override void Action(I element) => Action(index, element);
        protected virtual void Action(int index, I element) {}

        protected override void Dispose(I undo, I redo) => Dispose(index, undo, redo);
        protected virtual void Dispose(int index, I undo, I redo) {}

        public SimpleIndexUndoEntry(string desc, int index, I undo, I redo)
        : base(desc, undo, redo) => this.index = index;
    }
}