namespace Apollo.Undo {
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
}