using System.Collections.Generic;

using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Core {
    public interface IChainParent {}

    public interface IMultipleChainParent: IChainParent {
        IMultipleChainParentViewer SpecificViewer { get; }

        Chain this[int index] { get; }
        int Count { get; }

        void Insert(int index, Chain chain = null);
        void Remove(int index, bool dispose = true);
    }

    public interface IMultipleChainParentViewer {
        void Contents_Insert(int index, Chain chain);
        void Contents_Remove(int index);

        void Expand(int? index);
    }

    public interface ISelect {
        ISelectViewer IInfo { get; }

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
        void Rename(int left, int right);
    }
}