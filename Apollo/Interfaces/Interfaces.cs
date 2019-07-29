using System.Collections.Generic;

using Apollo.Elements;

namespace Apollo.Interfaces {
    public interface IChainParent {}

    public interface IMultipleChainParent: IChainParent {
        IMultipleChainParentViewer SpecificViewer { get; }

        Chain this[int index] { get; }
        int Count { get; }

        int? Expanded { get; }

        void Insert(int index, Chain chain = null);
        void Remove(int index, bool dispose = true);
    }

    public interface IMultipleChainParentViewer: ISelectParentViewer {
        void Contents_Insert(int index, Chain chain);
        void Contents_Remove(int index);
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

        bool IRoot { get; }
    }

    public interface ISelectParentViewer {
        int? IExpanded { get; }
        void Expand(int? index);

        void Copy(int left, int right, bool cut = false);
        void Duplicate(int left, int right);
        void Paste(int right);
        void Delete(int left, int right);
        void Group(int left, int right);
        void Ungroup(int left);
        void Mute(int left, int right);
        void Rename(int left, int right);
        void Export(int left, int right);
        void Import(int right, string path = null);
    }
}