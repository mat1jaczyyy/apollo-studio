using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Input;

using Apollo.Enums;

namespace Apollo.Selection {
    public interface ISelect {
        ISelectViewer IInfo { get; }

        ISelectParent IParent { get; }
        int? IParentIndex { get; }

        ISelect IClone(PurposeType purpose);
        
        void Dispose();
    }

    public interface ISelectViewer {
        void Deselect();
        void Select();

        void Select(PointerPressedEventArgs e);
        bool Selected { get; }
    }

    public interface ISelectParent {
        ISelectParentViewer IViewer { get; }

        List<ISelect> IChildren { get; }
        int Count { get; }

        bool IRoot { get; }

        void IInsert(int index, ISelect device);
        void Remove(int index, bool dispose = true);

        Window IWindow { get; }
        SelectionManager Selection { get; }

        Type ChildType { get; }
        string ChildString { get; }
        string ChildFileExtension { get; }
    }

    public interface ISelectParentViewer {
        int? IExpanded { get; }
        void Expand(int? index);
    }
    
    public interface IDroppable: IControl {
        List<string> DropAreas { get; }

        Dictionary<string, DragDropManager.DropHandler> DropHandlers { get; }

        ISelect Item { get; }
        ISelectParent ItemParent { get; }

        public bool DropLeft(IControl source, DragEventArgs e)
            => source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2;
    }

    public interface IDraggable: IDroppable, ISelectViewer {
        string DragFormat { get; }

        void DragFailed(PointerPressedEventArgs e);
    }

    public interface IMutable: ISelect {
        bool Enabled { get; set; }
    }

    public interface IName: ISelect {
        string Name { get; set; }
        string ProcessedName { get; }
    }

    public interface IRenamable: IControl, ISelectViewer {
        RenameManager Rename { get; }

        ISelect Item { get; }
        ISelectParent ItemParent { get; }

        TextBox Input { get; }
        TextBlock NameText { get; }
    }
}