using System;
using System.Linq;

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
        ComboBox ComboBox;
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

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();

            if (IsArrangeValid && _multi.Expanded != null && index <= _multi.Expanded) _multi.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (IsArrangeValid && _multi.Expanded != null) {
                if (index < _multi.Expanded) _multi.Expanded--;
                else if (index == _multi.Expanded) Expand(null);
            }

            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public MultiViewer(Multi multi, DeviceViewer parent) {
            InitializeComponent();

            _multi = multi;
            _multi.Preprocess.ClearParentIndexChanged();

            _parent = parent;
            _parent.Border.CornerRadius = new CornerRadius(0, 5, 5, 0);
            _parent.Header.CornerRadius = new CornerRadius(0, 5, 0, 0);

            ComboBox = this.Get<ComboBox>("ComboBox");
            ComboBox.SelectedItem = _multi.Mode;

            _root = _parent.Root.Children;
            _root.Insert(0, new DeviceHead(parent));
            _root.Insert(1, new ChainViewer(_multi.Preprocess, true));

            Contents = this.Get<StackPanel>("Contents").Children;
            
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
            
            for (int i = 0; i < _multi.Count; i++) {
                _multi[i].ClearParentIndexChanged();
                Contents_Insert(i, _multi[i]);
            }

            if (_multi.Expanded != null) Expand_Insert(multi.Expanded.Value);
        }

        private void Expand_Insert(int index) {
            _root.Insert(3, new ChainViewer(_multi[index], true));
            _root.Insert(4, new DeviceTail(_parent));

            _parent.Border.CornerRadius = new CornerRadius(0);
            _parent.Header.CornerRadius = new CornerRadius(0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        private void Expand_Remove() {
            _root.RemoveAt(4);
            _root.RemoveAt(3);

            _parent.Border.CornerRadius = new CornerRadius(0, 5, 5, 0);
            _parent.Header.CornerRadius = new CornerRadius(0, 5, 0, 0);
            ((ChainInfo)Contents[_multi.Expanded.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;
        }

        private void Expand(int? index) {
            if (_multi.Expanded != null) {
                Expand_Remove();

                if (index == _multi.Expanded) {
                    _multi.Expanded = null;
                    return;
                }
            }

            if (index != null) Expand_Insert(index.Value);
            
            _multi.Expanded = index;
        }

        private void Chain_Insert(int index) {
            _multi.Insert(index, new Chain());
            Contents_Insert(index, _multi[index]);
            
            Expand(index);
        }

        private void Chain_InsertStart() => Chain_Insert(0);

        private void Chain_Remove(int index) {
            Contents_Remove(index);
            _multi.Remove(index);
        }

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) => _multi.Mode = (string)ComboBox.SelectedItem;
    }
}
