namespace Apollo.Undo {
    public class UndoEntry {
        public readonly string Description;

        public virtual void Undo() {}
        public virtual void Redo() {}
        public virtual void Dispose() {}

        public UndoEntry(string desc) => Description = desc;
    }
}