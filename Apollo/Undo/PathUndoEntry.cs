using System.Collections.Generic;
using System.Linq;

using Apollo.Elements;
using Apollo.Interfaces;

namespace Apollo.Undo {
    public class PathUndoEntry<T>: UndoEntry where T: ISelect {
        protected IEnumerable<List<int>> Paths;
        protected T[] Items => Paths.Select(i => Track.TraversePath<T>(i)).ToArray();

        public override void Undo() => UndoPath(Items);
        protected virtual void UndoPath(params T[] items) {}

        public override void Redo() => RedoPath(Items);
        protected virtual void RedoPath(params T[] items) {}

        public override void Dispose() => DisposePath(Items);
        protected virtual void DisposePath(params T[] items) {}

        public PathUndoEntry(string desc, params T[] children): base(desc) => Paths = children.Select(i => Track.GetPath(i));
    }
}