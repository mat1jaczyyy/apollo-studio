using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class CopyViewer: UserControl {
        public static readonly string DeviceIdentifier = "copy";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Rate = this.Get<Dial>("Rate");
            Gate = this.Get<Dial>("Gate");
            Pinch = this.Get<Dial>("Pinch");

            CopyMode = this.Get<ComboBox>("CopyMode");
            GridMode = this.Get<ComboBox>("GridMode");
            Wrap = this.Get<CheckBox>("Wrap");

            Reverse = this.Get<CheckBox>("Reverse");
            Infinite = this.Get<CheckBox>("Infinite");
            
            Contents = this.Get<StackPanel>("Contents").Children;
            OffsetAdd = this.Get<HorizontalAdd>("OffsetAdd");
        }

        Copy _copy;

        Dial Rate, Gate, Pinch;
        ComboBox CopyMode, GridMode;
        CheckBox Wrap, Reverse, Infinite;

        Controls Contents;
        HorizontalAdd OffsetAdd;

        public void Contents_Insert(int index, Offset offset, int angle) {
            CopyOffset viewer = new CopyOffset(offset, angle, _copy);
            viewer.OffsetAdded += Offset_Insert;
            viewer.OffsetRemoved += Offset_Remove;

            Contents.Insert(index + 1, viewer);
            OffsetAdd.AlwaysShowing = false;
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            if (Contents.Count == 1) OffsetAdd.AlwaysShowing = true;
        }

        public CopyViewer() => new InvalidOperationException();

        public CopyViewer(Copy copy) {
            InitializeComponent();

            _copy = copy;

            Rate.UsingSteps = _copy.Time.Mode;
            Rate.Length = _copy.Time.Length;
            Rate.RawValue = _copy.Time.Free;

            Gate.RawValue = _copy.Gate * 100;
            Pinch.RawValue = _copy.Pinch;

            Reverse.IsChecked = _copy.Reverse;
            Infinite.IsChecked = _copy.Infinite;

            GridMode.SelectedIndex = (int)_copy.GridMode;
            
            SetCopyMode(_copy.CopyMode);

            Wrap.IsChecked = _copy.Wrap;

            for (int i = 0; i < _copy.Offsets.Count; i++)
                Contents_Insert(i, _copy.Offsets[i], _copy.GetAngle(i));
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _copy = null;

        void Rate_ValueChanged(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Rate Changed to {r}{Rate.Unit}", () => {
                    ((Copy)Track.TraversePath(path)).Time.Free = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Time.Free = r;
                });
            }

            _copy.Time.Free = (int)value;
        }

        public void SetRateValue(int rate) => Rate.RawValue = rate;

        void Rate_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Rate Switched to {(r? "Steps" : "Free")}", () => {
                    ((Copy)Track.TraversePath(path)).Time.Mode = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Time.Mode = r;
                });
            }

            _copy.Time.Mode = value;
        }

        public void SetMode(bool mode) => Rate.UsingSteps = mode;

        void Rate_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Rate Changed to {Length.Steps[r]}", () => {
                    ((Copy)Track.TraversePath(path)).Time.Length.Step = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Time.Length.Step = r;
                });
            }
        }

        public void SetRateStep(Length rate) => Rate.Length = rate;

        void Gate_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Gate Changed to {value}{Gate.Unit}", () => {
                    ((Copy)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Gate = r;
                });
            }

            _copy.Gate = value / 100;
        }

        public void SetGate(double gate) => Gate.RawValue = gate * 100;

        void CopyMode_Changed(object sender, SelectionChangedEventArgs e) {
            CopyType selected = (CopyType)CopyMode.SelectedIndex;

            if (_copy.CopyMode != selected) {
                CopyType u = _copy.CopyMode;
                CopyType r = selected;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Mode Changed to {((ComboBoxItem)CopyMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    ((Copy)Track.TraversePath(path)).CopyMode = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).CopyMode = r;
                });

                _copy.CopyMode = selected;
            }

            Rate.Enabled = Gate.Enabled = selected != CopyType.Static && selected != CopyType.RandomSingle;
            Pinch.Enabled = Reverse.IsEnabled = Infinite.IsEnabled = selected == CopyType.Animate || selected == CopyType.Interpolate;
            
            for (int i = 1; i < Contents.Count; i++)
                ((CopyOffset)Contents[i]).AngleEnabled = selected == CopyType.Interpolate;
        }

        public void SetCopyMode(CopyType mode) => CopyMode.SelectedIndex = (int)mode;

        void GridMode_Changed(object sender, SelectionChangedEventArgs e) {
            GridType selected = (GridType)GridMode.SelectedIndex;

            if (_copy.GridMode != selected) {
                GridType u = _copy.GridMode;
                GridType r = selected;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Grid Changed to {((ComboBoxItem)GridMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    ((Copy)Track.TraversePath(path)).GridMode = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).GridMode = r;
                });

                _copy.GridMode = selected;
            }
        }

        public void SetGridMode(GridType mode) => GridMode.SelectedIndex = (int)mode;

        void Pinch_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value;
                double r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Pinch Changed to {value}{Pinch.Unit}", () => {
                    ((Copy)Track.TraversePath(path)).Pinch = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Pinch = r;
                });
            }

            _copy.Pinch = value;
        }

        public void SetPinch(double pinch) => Pinch.RawValue = pinch;

        void Reverse_Changed(object sender, RoutedEventArgs e) {
            bool value = Reverse.IsChecked.Value;

            if (_copy.Reverse != value) {
                bool u = _copy.Reverse;
                bool r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Reverse Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Copy)Track.TraversePath(path)).Reverse = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Reverse = r;
                });

                _copy.Reverse = value;
            }
        }

        public void SetReverse(bool value) => Reverse.IsChecked = value;

        void Infinite_Changed(object sender, RoutedEventArgs e) {
            bool value = Infinite.IsChecked.Value;

            if (_copy.Infinite != value) {
                bool u = _copy.Infinite;
                bool r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Infinite Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Copy)Track.TraversePath(path)).Infinite = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Infinite = r;
                });

                _copy.Infinite = value;
            }
        }

        public void SetInfinite(bool value) => Infinite.IsChecked = value;

        void Wrap_Changed(object sender, RoutedEventArgs e) {
            bool value = Wrap.IsChecked.Value;

            if (_copy.Wrap != value) {
                bool u = _copy.Wrap;
                bool r = value;
                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Wrap Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Copy)Track.TraversePath(path)).Wrap = u;
                }, () => {
                    ((Copy)Track.TraversePath(path)).Wrap = r;
                });

                _copy.Wrap = value;
            }
        }

        public void SetWrap(bool value) => Wrap.IsChecked = value;

        void Offset_InsertStart() => Offset_Insert(0);

        void Offset_Insert(int index) {
            List<int> path = Track.GetPath(_copy);

            Program.Project.Undo.Add($"Copy Offset {index + 1} Inserted", () => {
                ((Copy)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Copy)Track.TraversePath(path)).Insert(index);
            });

            _copy.Insert(index);
        }

        void Offset_Remove(int index) {
            Offset u = _copy.Offsets[index].Clone();
            List<int> path = Track.GetPath(_copy);

            Program.Project.Undo.Add($"Copy Offset {index + 1} Removed", () => {
                ((Copy)Track.TraversePath(path)).Insert(index, u.Clone());
            }, () => {
                ((Copy)Track.TraversePath(path)).Remove(index);
            }, () => {
                u.Dispose();
            });

            _copy.Remove(index);
        }

        public void SetOffset(int index, Offset offset) => ((CopyOffset)Contents[index + 1]).SetOffset(offset);
        
        public void SetOffsetAngle(int index, double angle) => ((CopyOffset)Contents[index + 1]).SetAngle(angle);
    }
}
