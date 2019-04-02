using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.DeviceViewers {
    public class GroupViewer: UserControl {
        public static readonly string DeviceIdentifier = "group";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Group _group;
        StackPanel _root;

        private Controls Contents;

        private void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            Contents.Insert(index + 1, viewer);
        }

        public GroupViewer(Group group, StackPanel root) {
            InitializeComponent();

            _group = group;
            _root = root;

            Contents = this.Get<StackPanel>("Contents").Children;
            
            for (int i = 0; i < _group.Count; i++)
                Contents_Insert(i, _group[i]);
        }

        private void Chain_Insert(int index) {
            _group.Insert(index, new Chain());
            Contents_Insert(index, _group[index]);
        }

        private void Chain_InsertStart() => Chain_Insert(0);
    }
}
