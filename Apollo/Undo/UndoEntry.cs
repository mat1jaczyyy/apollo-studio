using System;

namespace Apollo.Undo {
    public class UndoEntry {
        public string Description;
        public Action Undo;
        public Action Redo;
        Action DisposeAction;

        public UndoEntry(string desc, Action undo = null, Action redo = null, Action dispose = null) {
            Description = desc;
            Undo = undo?? (() => {});
            Redo = redo?? (() => {});
            DisposeAction = dispose?? (() => {});
        }

        public void Dispose() => DisposeAction?.Invoke();
    }
}