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
        static SelectionManager GetSelection(ISelectParent item) {
            if (item is Chain chain) return Track.Get(chain)?.Window?.Selection;
            else if (item is Group group) return Track.Get(group)?.Window?.Selection;
            else if (item is Project) return Program.Project.Window?.Selection;
            else if (item is Pattern pattern) return pattern.Window?.Selection;

            return null;
        }

        static Window GetWindow(ISelectParent item) {
            if (item is Chain chain) return Track.Get(chain)?.Window;
            else if (item is Group group) return Track.Get(group)?.Window;
            else if (item is Project) return Program.Project.Window;
            else if (item is Pattern pattern) return pattern.Window;

            return null;
        }

        ISelectParentViewer IViewer { get; }

        List<ISelect> IChildren { get; }
        int Count { get; }

        bool IRoot { get; }

        void IInsert(int index, ISelect device);
        void Remove(int index, bool dispose = true);
    }

    public interface ISelectParentViewer {
        int? IExpanded { get; }
        void Expand(int? index);
    }

    public interface IMutable: ISelect {
        bool Enabled { get; set; }
    }
}