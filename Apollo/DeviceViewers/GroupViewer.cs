using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.DeviceViewers {
    public class GroupViewer: UserControl {
        public static readonly string DeviceIdentifier = "group";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Group _group;
        DeviceViewer _parent;
        Controls _root;

        Controls Contents;
        VerticalAdd ChainAdd;

        int? current;

        public void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            viewer.ChainRemoved += Chain_Remove;
            viewer.ChainExpanded += Expand;

            Contents.Insert(index + 1, viewer);
            ChainAdd.AlwaysShowing = false;

            if (current != null && index <= current) current++;
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            if (Contents.Count == 1) ChainAdd.AlwaysShowing = true;

            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }
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
        }

        private void Expand(int? index) {
            if (current != null) {
                _root.RemoveAt(2);
                _root.RemoveAt(1);

                _parent.Border.CornerRadius = new CornerRadius(5);
                _parent.Header.CornerRadius = new CornerRadius(5, 5, 0, 0);
                ((ChainInfo)Contents[current.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                _root.Insert(1, new ChainViewer(_group[index.Value], true));
                _root.Insert(2, new DeviceTail(_parent));

                _parent.Border.CornerRadius = new CornerRadius(5, 0, 0, 5);
                _parent.Header.CornerRadius = new CornerRadius(5, 0, 0, 0);
                ((ChainInfo)Contents[index.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
            }
            
            current = index;
        }

        private void Chain_Insert(int index) {
            Chain chain = new Chain();
            if (Preferences.AutoCreatePageFilter) chain.Add(new PageFilter());
            if (Preferences.AutoCreateKeyFilter) chain.Add(new KeyFilter());

            _group.Insert(index, chain);
            Contents_Insert(index, _group[index]);
            
            Expand(index);
        }

        private void Chain_InsertStart() => Chain_Insert(0);

        private void Chain_Remove(int index) {
            Contents_Remove(index);
            _group.Remove(index);
        }
    }
}
