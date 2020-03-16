using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Input;

using Apollo.Selection;

namespace Apollo.DragDrop {
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
}