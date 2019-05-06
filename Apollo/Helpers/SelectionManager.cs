using System.Collections.Generic;
using System.Linq;

using Apollo.Core;

namespace Apollo.Helpers {
    public class SelectionManager {
        public ISelect SelectionStart { get; private set; } = null;
        public ISelect SelectionEnd { get; private set; } = null;

        public List<ISelect> Selection {
            get {
                if (SelectionStart != null) {
                    if (SelectionEnd != null) {
                        ISelect left = (SelectionStart.IParentIndex.Value < SelectionEnd.IParentIndex.Value)? SelectionStart : SelectionEnd;
                        ISelect right = (SelectionStart.IParentIndex.Value < SelectionEnd.IParentIndex.Value)? SelectionEnd : SelectionStart;

                        return left.IParent.IChildren.Skip(left.IParentIndex.Value).Take(right.IParentIndex.Value - left.IParentIndex.Value + 1).ToList();
                    }
                    
                    return new List<ISelect>() {SelectionStart};
                }

                return new List<ISelect>();
            }
        }

        public void Select(ISelect select, bool shift = false) {
            if (SelectionStart != null)
                if (SelectionEnd != null)
                    foreach (ISelect selected in Selection)
                        selected.IInfo?.Deselect();
                else SelectionStart.IInfo?.Deselect();

            if (shift && SelectionStart != null && SelectionStart.IParent == select.IParent && SelectionStart != select)
                SelectionEnd = select;

            else {
                SelectionStart = select;
                SelectionEnd = null;
            }

            if (SelectionStart != null)
                if (SelectionEnd != null)
                    foreach (ISelect selected in Selection)
                        selected.IInfo?.Select();
                else SelectionStart.IInfo?.Select();
        }

        public void Action(string action) {
            if (SelectionStart == null) return;

            ISelectParent parent = SelectionStart.IParent;
            
            int left = SelectionStart.IParentIndex.Value;
            int right = (SelectionEnd == null)? left: SelectionEnd.IParentIndex.Value;
            
            if (left > right) {
                int temp = left;
                left = right;
                right = temp;
            }

            Action(action, parent, left, right);
        }

        public void Action(string action, ISelectParent parent, int index) => Action(action, parent, index, index);

        public void Action(string action, ISelectParent parent, int left, int right) {
            if (action == "Cut") parent.IViewer?.Copy(left, right, true);
            else if (action == "Copy") parent.IViewer?.Copy(left, right);
            else if (action == "Duplicate") parent.IViewer?.Duplicate(left, right);
            else if (action == "Paste") parent.IViewer?.Paste(right);
            else if (action == "Delete") parent.IViewer?.Delete(left, right);
            else if (action == "Group") parent.IViewer?.Group(left, right);
            else if (action == "Ungroup") parent.IViewer?.Ungroup(left);
            else if (action == "Rename") parent.IViewer?.Rename(left, right);
        }
    }
}