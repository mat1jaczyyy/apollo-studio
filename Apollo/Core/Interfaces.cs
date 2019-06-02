using System;
using System.Collections.Generic;

using Avalonia.Controls;

using Apollo.Components;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Core {
    public interface IChainParent {}

    public interface IMultipleChainParent: IChainParent {
        IMultipleChainParentViewer SpecificViewer { get; }

        Chain this[int index] { get; }
        int Count { get; }

        int? Expanded { get; }

        void Insert(int index, Chain chain = null);
        void Remove(int index, bool dispose = true);
    }

    public interface IMultipleChainParentViewer {
        void Contents_Insert(int index, Chain chain);
        void Contents_Remove(int index);

        void Expand(int? index);
    }
    
    public delegate void DeviceAddedEventHandler(int index, Type device);
    public delegate void DeviceCollapsedEventHandler(int index);

    public interface IDeviceViewer: IControl, ISelectViewer {
        event DeviceAddedEventHandler DeviceAdded;
        event DeviceCollapsedEventHandler DeviceCollapsed;

        DeviceAdd DeviceAdd { get; }
        IControl SpecificViewer { get; }
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
        void Rename(int left, int right);
    }
}