using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Input;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Selection {
    public interface ISelect {
        static SelectionManager GetSelection(ISelect item, ISelectParent target) {
            if (item is Device device) return Track.Get(device)?.Window?.Selection;

            else if (item is Chain chain) {
                ((Group)chain.Parent).SpecificViewer.Expand(chain.ParentIndex);  // TODO this and Frame_Select are not nice
                return Track.Get(chain)?.Window?.Selection;
            
            } else if (item is Track track) return Program.Project.Window?.Selection;
            
            else if (item is Frame frame) {
                PatternWindow window = ((Pattern)target).Window;

                window?.Frame_Select(item.IParentIndex.Value);
                return window?.Selection;
            }

            return null;
        }

        ISelectViewer IInfo { get; }

        ISelectParent IParent { get; }
        int? IParentIndex { get; }

        ISelect IClone();
        
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