using System.Collections.Generic;

namespace Apollo.Core {
    public interface IChainParent {}

    public interface ISelect {
        ISelectViewer IViewer { get; }
        ISelectParent IParent { get; }
        int? IParentIndex { get; }
    }

    public interface ISelectViewer {
        void Deselect();
        void Select();
    }

    public interface ISelectParent {
        ISelectParentViewer IViewer { get; }
        List<ISelect> IChildren { get; }
    }

    public interface ISelectParentViewer {
        void Copy(int left, int right, bool cut = false);
        void Duplicate(int left, int right);
        void Paste(int right);
        void Delete(int left, int right);
        void Group(int left, int right);
        void Ungroup(int left);
    }
}