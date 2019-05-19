using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class CopyViewer: UserControl {
        public static readonly string DeviceIdentifier = "copy";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Copy _copy;

        ComboBox CopyMode, GridMode;
        CheckBox Wrap;
        Dial Rate, Gate;

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

            CopyMode = this.Get<ComboBox>("CopyMode");
            CopyMode.SelectedItem = _copy.CopyMode;

            GridMode = this.Get<ComboBox>("GridMode");
            GridMode.SelectedItem = _copy.GridMode;

            Wrap = this.Get<CheckBox>("Wrap");
            Wrap.IsChecked = _copy.Wrap;

            Contents = this.Get<StackPanel>("Contents").Children;
            OffsetAdd = this.Get<HorizontalAdd>("OffsetAdd");

            for (int i = 0; i < _copy.Offsets.Count; i++)
                Contents_Insert(i, _copy.Offsets[i]);
        }

        private void Rate_ValueChanged(double value, double? old) {
            if (old != null) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Rate Changed", () => {
                    ((Copy)Track.TraversePath(path)).Rate = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Rate = r;
                });
            }

            _copy.Rate = (int)value;
        }

        public void SetRateValue(int rate) => Rate.RawValue = rate;

        private void Rate_ModeChanged(bool value, bool? old) {
            if (old != null) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Rate Switched", () => {
                    ((Copy)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Mode = r;
                });
            }

            _copy.Mode = value;
        }

        public void SetMode(bool mode) => Rate.UsingSteps = mode;

        private void Rate_StepChanged(int value, int? old) {
            if (old != null) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Rate Changed", () => {
                    ((Copy)Track.TraversePath(path)).Length.Step = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Length.Step = r;
                });
            }
        }

        public void SetRateStep(int rate) => Rate.DrawArcAuto();

        private void Gate_Changed(double value, double? old) {
            if (old != null) {
                decimal u = (decimal)(old.Value / 100);
                decimal r = (decimal)(value / 100);
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Gate Changed", () => {
                    ((Copy)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Gate = r;
                });
            }

            _copy.Gate = (decimal)(value / 100);
        }

        public void SetGate(decimal gate) => Gate.RawValue = (double)gate * 100;

        private void CopyMode_Changed(object sender, SelectionChangedEventArgs e) {
            string selected = (string)CopyMode.SelectedItem;

            if (_copy.CopyMode != selected) {
                string u = _copy.CopyMode;
                string r = selected;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Mode Changed", () => {
                    ((Copy)Track.TraversePath(path)).CopyMode = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).CopyMode = r;
                });

                _copy.CopyMode = selected;
            }

            Rate.Enabled = Gate.Enabled = CopyMode.SelectedIndex > 0;
        }

        public void SetCopyMode(string mode) => CopyMode.SelectedItem = mode;

        private void GridMode_Changed(object sender, SelectionChangedEventArgs e) {
            string selected = (string)GridMode.SelectedItem;

            if (_copy.GridMode != selected) {
                string u = _copy.GridMode;
                string r = selected;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Grid Changed", () => {
                    ((Copy)Track.TraversePath(path)).GridMode = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).GridMode = r;
                });

                _copy.GridMode = selected;
            }
        }

        public void SetGridMode(string mode) => GridMode.SelectedItem = mode;

        private void Wrap_Changed(object sender, EventArgs e) {
            bool value = Wrap.IsChecked.Value;

            if (_copy.Wrap != value) {
                bool u = _copy.Wrap;
                bool r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Wrap Changed", () => {
                    ((Copy)Track.TraversePath(path)).Wrap = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Wrap = r;
                });

                _copy.Wrap = value;
            }
        }

        public void SetWrap(bool value) => Wrap.IsChecked = value;

        private void Offset_InsertStart() => Offset_Insert(0);

        private void Offset_Insert(int index) {
            List<int> path = Track.GetPath(_copy);

            Program.Project.Undo.Add($"Copy Offset Added", () => {
                ((Copy)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Copy)Track.TraversePath(path)).Insert(index);
            });

            _copy.Insert(index);
        }

        private void Offset_Remove(int index) {
            Offset u = _copy.Offsets[index].Clone();
            List<int> path = Track.GetPath(_copy);

            Program.Project.Undo.Add($"Copy Offset Deleted", () => {
                ((Copy)Track.TraversePath(path)).Insert(index, u.Clone());
            }, () => {
                ((Copy)Track.TraversePath(path)).Remove(index);
            });

            _copy.Remove(index);
        }

        public void SetOffset(int index, int x, int y) => ((CopyOffset)Contents[index + 1]).SetOffset(x, y);
    }
}
