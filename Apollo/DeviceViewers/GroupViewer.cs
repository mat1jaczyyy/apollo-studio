using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.DeviceViewers {
    public class GroupViewer: UserControl, IMultipleChainParentViewer, ISelectParentViewer {
        public static readonly string DeviceIdentifier = "group";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Group _group;
        DeviceViewer _parent;
        Controls _root;

        Controls Contents;
        VerticalAdd ChainAdd;

        private void SetAlwaysShowing() {
            ChainAdd.AlwaysShowing = (Contents.Count == 1);

            for (int i = 1; i < Contents.Count; i++)
                ((ChainInfo)Contents[i]).ChainAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((ChainInfo)Contents.Last()).ChainAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            viewer.ChainRemoved += Chain_Remove;
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

        public GroupViewer(Group group, DeviceViewer parent) {
            InitializeComponent();

            _group = group;

            _parent = parent;

            _root = _parent.Root.Children;

            Contents = this.Get<StackPanel>("Contents").Children;
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
            
            for (int i = 0; i < _group.Count; i++) {
                _group[i].ClearParentIndexChanged();
                Contents_Insert(i, _group[i]);
            }

            if (_group.Expanded != null) Expand_Insert(_group.Expanded.Value);
        }

        private void Expand_Insert(int index) {
            _root.Insert(1, new ChainViewer(_group[index], true));
            _root.Insert(2, new DeviceTail(_parent));

            _parent.Border.CornerRadius = new CornerRadius(5, 0, 0, 5);
            _parent.Header.CornerRadius = new CornerRadius(5, 0, 0, 0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        private void Expand_Remove() {
            _root.RemoveAt(2);
            _root.RemoveAt(1);

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

        private void Chain_Insert(int index) {
            Chain chain = new Chain();
            if (Preferences.AutoCreatePageFilter) chain.Add(new PageFilter());
            if (Preferences.AutoCreateKeyFilter) chain.Add(new KeyFilter());

            Chain_Insert(index, chain);
        }

        private void Chain_Insert(int index, Chain chain) {
            _group.Insert(index, chain);
            Contents_Insert(index, _group[index]);
            
            Track.Get(chain).Window?.Select(chain);
            Expand(index);
        }

        private void Chain_InsertStart() => Chain_Insert(0);

        private void Chain_Remove(int index) {
            Contents_Remove(index);
            _group.Remove(index);
        }

        private void Chain_Action(string action) => Chain_Action(action, false);
        private void Chain_Action(string action, bool right) => Track.Get(_group).Window?.SelectionAction(action, _group, (right? _group.Count : 0) - 1);

        public async void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_group[i]);

            string b64 = Convert.ToBase64String(Encoder.Encode(copy).ToArray());

            if (cut) Delete(left, right);
            
            await Application.Current.Clipboard.SetTextAsync(b64);
        }

        public async void Paste(int right) {
            string b64 = await Application.Current.Clipboard.GetTextAsync();
            
            Copyable paste = Decoder.Decode(new MemoryStream(Convert.FromBase64String(b64)), typeof(Copyable));
            
            for (int i = 0; i < paste.Contents.Count; i++)
                Chain_Insert(right + i + 1, (Chain)paste.Contents[i]);
        }

        public void Duplicate(int left, int right) {
            for (int i = 0; i <= right - left; i++)
                Chain_Insert(right + i + 1, (Chain)_group[left + i].Clone());
        }

        public void Delete(int left, int right) {
            for (int i = right; i >= left; i--)
                Chain_Remove(i);
        }

        public void Group(int left, int right) => throw new InvalidOperationException("A Chain cannot be grouped.");

        public void Ungroup(int index) => throw new InvalidOperationException("A Chain cannot be ungrouped.");
    }
}
