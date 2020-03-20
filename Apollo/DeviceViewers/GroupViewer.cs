using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.DragDrop;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Selection;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class GroupViewer: UserControl, ISelectParentViewer, IDroppable {
        public static readonly string DeviceIdentifier = "group";

        public int? IExpanded {
            get => _group.Expanded;
        }

        protected virtual void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Contents = this.Get<StackPanel>("Contents").Children;
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
        }
        
        protected Group _group;
        protected DeviceViewer _parent;
        protected Controls _root;

        protected Controls Contents;
        protected VerticalAdd ChainAdd;

        protected void SetAlwaysShowing() {
            ChainAdd.AlwaysShowing = (Contents.Count == 1);

            for (int i = 1; i < Contents.Count; i++)
                ((ChainInfo)Contents[i]).ChainAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((ChainInfo)Contents.Last()).ChainAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            viewer.ChainExpanded += Expand;
            chain.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();

            if (IsArrangeValid && _group.Expanded != null && index <= _group.Expanded) _group.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (IsArrangeValid && _group.Expanded != null) {
                if (index < _group.Expanded) _group.Expanded--;
                else if (index == _group.Expanded) Expand(null);
            }

            _group[index].Info = null;
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public GroupViewer() => new InvalidOperationException();

        public GroupViewer(Group group, DeviceViewer parent) {
            InitializeComponent();

            _group = group;
            _parent = parent;
            _root = _parent.Root.Children;

            DragDrop = new DragDropManager(this);
            
            for (int i = 0; i < _group.Count; i++) {
                _group[i].ClearParentIndexChanged();
                Contents_Insert(i, _group[i]);
            }

            if (_group.Expanded != null) Expand_Insert(_group.Expanded.Value);
        }

        protected void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            DragDrop.Dispose();
            DragDrop = null;

            _group = null;
            _parent = null;
            _root = null;
        }

        protected int ExpandBase = 1;

        protected virtual void Expand_Insert(int index) {
            _root.Insert(ExpandBase, new ChainViewer(_group[index], true));
            _root.Insert(ExpandBase + 1, new DeviceTail(_group, _parent));

            _parent.Border.CornerRadius = new CornerRadius(5, 0, 0, 5);
            _parent.Header.CornerRadius = new CornerRadius(5, 0, 0, 0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        protected virtual void Expand_Remove() {
            _root.RemoveAt(ExpandBase + 1);
            _root.RemoveAt(ExpandBase);

            _parent.Border.CornerRadius = new CornerRadius(5);
            _parent.Header.CornerRadius = new CornerRadius(5, 5, 0, 0);
            ((ChainInfo)Contents[_group.Expanded.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;
        }

        public void Expand(int? index) {
            if (_group.Expanded != null) {
                Expand_Remove();

                if (index == _group.Expanded) {
                    _group.Expanded = null;
                    return;
                }
            }

            if (index != null) Expand_Insert(index.Value);
            
            _group.Expanded = index;
        }

        protected void Chain_Insert(int index) {
            Chain chain = new Chain();

            if (this.GetType() == typeof(Group)) {
                if (Preferences.AutoCreateMacroFilter) chain.Add(new MacroFilter());
                if (Preferences.AutoCreateKeyFilter) chain.Add(new KeyFilter());
                if (Preferences.AutoCreatePattern) chain.Add(new Pattern());
            }

            Chain_Insert(index, chain);
        }

        protected void Chain_InsertStart() => Chain_Insert(0);

        protected void Chain_Insert(int index, Chain chain) {
            Program.Project.Undo.AddAndExecute(new Group.ChainInsertedUndoEntry(
                _group,
                index,
                chain
            ));
        }

        protected void Chain_Action(string action) => Chain_Action(action, false);
        protected void Chain_Action(string action, bool right) => Track.Get(_group)?.Window?.Selection.Action(action, _group, (right? _group.Count : 0) - 1);

        protected void ContextMenu_Action(string action) => Chain_Action(action, true);

        protected void Click(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
                ((ApolloContextMenu)this.Resources["ChainContextMenu"]).Open((Control)sender);

            e.Handled = true;
        }

        DragDropManager DragDrop;

        public List<string> DropAreas => new List<string>() {"DropZoneAfter", "ChainAdd"};

        public Dictionary<string, DragDropManager.DropHandler> DropHandlers => new Dictionary<string, DragDropManager.DropHandler>() {
            {DataFormats.FileNames, null},
            {"Chain", null},
            {"Device", ChainInfo.DeviceAsChainDrop}
        };

        public ISelect Item => null;
        public ISelectParent ItemParent => _group;
        
        public void Rename(int left, int right) => ((ChainInfo)Contents[left + 1]).StartInput(left, right);
    }
}
