using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class HoldViewer: UserControl {
        public static readonly string DeviceIdentifier = "hold";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Hold _hold;

        Dial Duration, Gate;
        CheckBox Infinite, Release;

        public HoldViewer(Hold hold) {
            InitializeComponent();

            _hold = hold;

            Duration = this.Get<Dial>("Duration");
            Duration.UsingSteps = _hold.Mode;
            Duration.Length = _hold.Length;
            Duration.RawValue = _hold.Time;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_hold.Gate * 100;

            Infinite = this.Get<CheckBox>("Infinite");
            Infinite.IsChecked = _hold.Infinite;
            Infinite_Changed(null, EventArgs.Empty); // required to set Dial Enabled properties

            Release = this.Get<CheckBox>("Release");
            Release.IsChecked = _hold.Release;
        }

        private void Duration_Changed(double value, double? old) {
            if (old != null) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Duration Changed", () => {
                    ((Hold)Track.TraversePath(path)).Time = u;
                }, () => {
                    ((Hold)Track.TraversePath(path)).Time = r;
                });
            }

            _hold.Time = (int)value;
        }

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        private void Duration_ModeChanged(bool value, bool? old) {
            if (old != null) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Duration Switched", () => {
                    ((Hold)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Hold)Track.TraversePath(path)).Mode = r;
                });
            }

            _hold.Mode = value;
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        private void Duration_StepChanged(int value, int? old) {
            if (old != null) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Duration Changed", () => {
                    ((Hold)Track.TraversePath(path)).Length.Step = u;
                }, () => {
                    ((Hold)Track.TraversePath(path)).Length.Step = r;
                });
            }
        }

        public void SetDurationStep(int duration) => Duration.DrawArcAuto();

        private void Gate_Changed(double value, double? old) {
            if (old != null) {
                decimal u = (decimal)(old.Value / 100);
                decimal r = (decimal)(value / 100);
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Gate Changed", () => {
                    ((Hold)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Hold)Track.TraversePath(path)).Gate = r;
                });
            }

            _hold.Gate = (decimal)(value / 100);
        }

        public void SetGate(decimal gate) => Gate.RawValue = (double)gate * 100;

        private void Infinite_Changed(object sender, EventArgs e) {
            bool value = Infinite.IsChecked.Value;

            if (_hold.Infinite != value) {
                bool u = _hold.Infinite;
                bool r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Infinite Changed", () => {
                    ((Hold)Track.TraversePath(path)).Infinite = u;
                }, () => {
                    ((Hold)Track.TraversePath(path)).Infinite = r;
                });

                _hold.Infinite = value;
                Duration.Enabled = Gate.Enabled = !value;
            }
        }

        public void SetInfinite(bool value) => Infinite.IsChecked = value;

        private void Release_Changed(object sender, EventArgs e) {
            bool value = Release.IsChecked.Value;

            if (_hold.Release != value) {
                bool u = _hold.Release;
                bool r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Release Changed", () => {
                    ((Hold)Track.TraversePath(path)).Release = u;
                }, () => {
                    ((Hold)Track.TraversePath(path)).Release = r;
                });

                _hold.Release = value;
            }
        }

        public void SetRelease(bool value) => Release.IsChecked = value;
    }
}
