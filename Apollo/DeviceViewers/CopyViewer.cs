using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class CopyViewer: UserControl {
        public static readonly string DeviceIdentifier = "copy";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Copy _copy;

        ComboBox ComboBox;
        Dial Rate, Gate;
        CheckBox Loop;

        Controls Contents;
        HorizontalAdd OffsetAdd;

        public void Contents_Insert(int index, Offset offset) {
            CopyOffset viewer = new CopyOffset(offset, _copy);
            viewer.OffsetAdded += Offset_Insert;
            viewer.OffsetRemoved += Offset_Remove;

            Contents.Insert(index + 1, viewer);
            OffsetAdd.AlwaysShowing = false;
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            if (Contents.Count == 1) OffsetAdd.AlwaysShowing = true;
        }

        public CopyViewer(Copy copy) {
            InitializeComponent();

            _copy = copy;

            Rate = this.Get<Dial>("Rate");
            Rate.UsingSteps = _copy.Mode;
            Rate.Length = _copy.Length;
            Rate.RawValue = _copy.Rate;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_copy.Gate * 100;

            ComboBox = this.Get<ComboBox>("ComboBox");
            ComboBox.SelectedItem = _copy.CopyMode;
            CopyMode_Changed(null, null);

            Loop = this.Get<CheckBox>("Loop");
            Loop.IsChecked = _copy.Loop;
            Loop_Changed(null, EventArgs.Empty);

            Contents = this.Get<StackPanel>("Contents").Children;
            OffsetAdd = this.Get<HorizontalAdd>("OffsetAdd");

            for (int i = 0; i < _copy.Offsets.Count; i++)
                Contents_Insert(i, _copy.Offsets[i]);
        }

        private void Rate_Changed(double value) => _copy.Rate = (int)value;

        private void Rate_ModeChanged(bool value) => _copy.Mode = value;

        private void Gate_Changed(double value) => _copy.Gate = (decimal)(value / 100);

        private void CopyMode_Changed(object sender, SelectionChangedEventArgs e) {
            _copy.CopyMode = (string)ComboBox.SelectedItem;
            Rate.Enabled = Gate.Enabled = ComboBox.SelectedIndex > 0;
        }

        private void Loop_Changed(object sender, EventArgs e) => _copy.Loop = Loop.IsChecked.Value;

        private void Offset_Insert(int index) {
            _copy.Offsets.Insert(index, new Offset());
            Contents_Insert(index, _copy.Offsets[index]);
        }

        private void Offset_InsertStart() => Offset_Insert(0);

        private void Offset_Remove(int index) {
            Contents_Remove(index);
            _copy.Offsets.RemoveAt(index);
        }
    }
}
