using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Apollo.Core;
using Apollo.Undo;

namespace Apollo.Selection {
    public class RenameManager {
        IRenamable Host;
        IDisposable observable;
        
        int Left, Right;
        List<string> Clean;
        bool Ignore = false;

        public RenameManager(IRenamable control) {
            Host = control;

            Host.Input.LostFocus += LostFocus;
            Host.Input.KeyDown += KeyDown;
            Host.Input.KeyUp += KeyUp;
            Host.Input.PointerReleased += MouseUp;

            observable = Host.Input.GetObservable(TextBox.TextProperty).Subscribe(Changed);
        }

        void Changed(string text) {
            if (Ignore || text == null || text == "") return;

            Ignore = true;

            for (int i = Left; i <= Right; i++)
                ((IName)Host.ItemParent.IChildren[i]).Name = text;

            Ignore = false;
        }

        void LostFocus(object sender, RoutedEventArgs e) {
            Host.Input.Text = ((IName)Host.Item).Name;

            Host.Input.Opacity = 0;
            Host.Input.IsHitTestVisible = false;
            
            List<string> newName = (from i in Enumerable.Range(0, Clean.Count) select Host.Input.Text).ToList();

            if (!newName.SequenceEqual(Clean))
                Program.Project.Undo.Add(new RenamedUndoEntry(Host.ItemParent, Left, Right, Clean, newName));
        }

        void KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return)
                Host.Focus();

            e.Key = Key.None;
        }

        void KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;

        public void StartInput(int left, int right) {
            Left = left;
            Right = right;
            Clean = new List<string>();

            for (int i = Left; i <= Right; i++)
                Clean.Add(((IName)Host.ItemParent.IChildren[i]).Name);
            
            Host.Input.Text = ((IName)Host.Item).Name;
            Host.Input.SelectionStart = 0;
            Host.Input.SelectionEnd = Host.Input.Text.Length;
            Host.Input.CaretIndex = Host.Input.Text.Length;

            Host.Input.Opacity = 1;
            Host.Input.IsHitTestVisible = true;
            Host.Input.Focus();
        }

        public void UpdateText() => Host.NameText.Text = ((IName)Host.Item).ProcessedName;

        public void SetName(string name) {
            UpdateText();

            if (Ignore) return;

            Ignore = true;
            Host.Input.Text = name;
            Ignore = false;
        }

        public void Dispose() {
            observable.Dispose();

            Host.Input.LostFocus -= LostFocus;
            Host.Input.KeyDown -= KeyDown;
            Host.Input.KeyUp -= KeyUp;
            Host.Input.PointerReleased -= MouseUp;

            Host = null;
        }

        public class RenamedUndoEntry: SimplePathUndoEntry<ISelectParent, List<string>> {
            int left, right;

            protected override void Action(ISelectParent item, List<string> element) {
                for (int i = left; i <= right; i++)
                    ((IName)item.IChildren[i]).Name = element[i - left];
                
                item.Selection?.Select(item.IChildren[left]);
                item.Selection?.Select(item.IChildren[right], true);
            }

            public RenamedUndoEntry(ISelectParent parent, int left, int right, List<string> undo, List<string> redo)
            : base($"{parent.ChildString} Renamed to {redo[0]}", parent, undo.ToList(), redo.ToList()) {
                this.left = left;
                this.right = right;
            }
        }
    }
}