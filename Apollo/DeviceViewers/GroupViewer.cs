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

        int? current;

        private void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            viewer.ChainRemoved += Chain_Remove;
            viewer.ChainExpanded += Expand;
            Contents.Insert(index + 1, viewer);
        }

        public GroupViewer(Group group, DeviceViewer parent) {
            InitializeComponent();

            _group = group;
            _parent = parent;
            _root = _parent.Get<StackPanel>("Root").Children;

            Contents = this.Get<StackPanel>("Contents").Children;

            if (_group.Count == 0) this.Get<ChainAdd>("ChainAdd").AlwaysShowing = true;
            
            for (int i = 0; i < _group.Count; i++)
                Contents_Insert(i, _group[i]);
            
            parent.Get<Grid>("Contents").Margin = new Thickness(0);
        }

        private void Expand(int? index) {
            if (current != null) {
                _root.RemoveAt(2);
                _root.RemoveAt(1);

                _parent.Get<Border>("Border").CornerRadius = new CornerRadius(5);
                ((ChainInfo)Contents[current.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                _root.Insert(1, new ChainViewer(_group[index.Value]) { Background = new SolidColorBrush(new Color(16, 0, 0, 0)) });
                _root.Insert(2, new GroupTail());

                _parent.Get<Border>("Border").CornerRadius = new CornerRadius(5, 0, 0, 5);
                ((ChainInfo)Contents[index.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
            }
            
            current = index;
        }

        private void Chain_Insert(int index) {
            Chain chain = new Chain();
            if (Preferences.AutoCreateFilter) chain.Add(new KeyFilter());

            _group.Insert(index, chain);
            Contents_Insert(index, _group[index]);

            Expand(index);

            this.Get<ChainAdd>("ChainAdd").AlwaysShowing = false;
        }

        private void Chain_InsertStart() => Chain_Insert(0);

        private void Chain_Remove(int index) {
            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }

            Contents.RemoveAt(index + 1);
            _group.Remove(index);

            if (_group.Count == 0) this.Get<ChainAdd>("ChainAdd").AlwaysShowing = true;
        }
    }
}
