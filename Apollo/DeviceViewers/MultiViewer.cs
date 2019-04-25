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
    public class MultiViewer: UserControl {
        public static readonly string DeviceIdentifier = "multi";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Multi _multi;
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

        public MultiViewer(Multi multi, DeviceViewer parent) {
            InitializeComponent();

            _multi = multi;
            _multi.Preprocess.ClearParentIndexChanged();

            _parent = parent;
            _parent.Border.CornerRadius = new CornerRadius(0, 5, 5, 0);
            _parent.Header.CornerRadius = new CornerRadius(0, 5, 0, 0);

            _root = _parent.Root.Children;
            _root.Insert(0, new DeviceHead((IBrush)_parent.Resources["TitleBrush"]));
            _root.Insert(1, new ChainViewer(_multi.Preprocess, true));

            Contents = this.Get<StackPanel>("Contents").Children;
            
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
            
            for (int i = 0; i < _multi.Count; i++) {
                _multi[i].ClearParentIndexChanged();
                Contents_Insert(i, _multi[i]);
            }
        }

        private void Expand(int? index) {
            if (current != null) {
                _root.RemoveAt(4);
                _root.RemoveAt(3);

                _parent.Border.CornerRadius = new CornerRadius(0, 5, 5, 0);
                _parent.Header.CornerRadius = new CornerRadius(0, 5, 0, 0);
                ((ChainInfo)Contents[current.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                _root.Insert(3, new ChainViewer(_multi[index.Value], true));
                _root.Insert(4, new DeviceTail(_parent.Header.Background));

                _parent.Border.CornerRadius = new CornerRadius(0);
                _parent.Header.CornerRadius = new CornerRadius(0);
                ((ChainInfo)Contents[index.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
            }
            
            current = index;
        }

        private void Chain_Insert(int index) {
            _multi.Insert(index, new Chain());
            Contents_Insert(index, _multi[index]);
            
            Expand(index);
        }

        private void Chain_InsertStart() => Chain_Insert(0);

        private void Chain_Remove(int index) {
            _multi.Remove(index);
            Contents_Remove(index);
        }
    }
}
