using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Selection;

namespace Apollo.Undo {
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
        protected T[] Items => Paths.Select(i => ((T)i?.Resolve())?? (T)(ISelectParent)Program.Project).ToArray();

        public override void Undo() => UndoPath(Items);
        protected virtual void UndoPath(params T[] items) {}

        public override void Redo() => RedoPath(Items);
        protected virtual void RedoPath(params T[] items) {}

        public PathParentUndoEntry(string desc, params T[] items)
        : base(desc) => Paths = items.Select(i => (i is Project)? null : new Path<ISelect>((ISelect)i)).ToList();
    }
}