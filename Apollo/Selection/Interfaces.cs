using System.Collections.Generic;

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
            else if (item is Chain chain) return Track.Get(chain)?.Window?.Selection;
            else if (item is Track track) return Program.Project.Window?.Selection;
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
    }

    public interface ISelectParentViewer {
        int? IExpanded { get; }
        void Expand(int? index);

        void Copy(int left, int right, bool cut = false);
        void Duplicate(int left, int right);
        void Paste(int right);
        void Replace(int left, int right);
        void Delete(int left, int right);

        void Group(int left, int right);
        void Ungroup(int left);
        void Choke(int left, int right);
        void Unchoke(int left);

        void Mute(int left, int right);
        void Rename(int left, int right);
        
        void Export(int left, int right);
        void Import(int right, string path = null);
    }
}