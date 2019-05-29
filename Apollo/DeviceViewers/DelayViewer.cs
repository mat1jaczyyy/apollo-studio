using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class DelayViewer: UserControl {
        public static readonly string DeviceIdentifier = "delay";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Delay _delay;
        Dial Duration, Gate;

        public DelayViewer(Delay delay) {
            InitializeComponent();

            _delay = delay;
            Duration = this.Get<Dial>("Duration");
            Duration.UsingSteps = _delay.Time.Mode;
            Duration.Length = _delay.Time.Length;
            Duration.RawValue = _delay.Time.Free;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_delay.Gate * 100;
        }

        private void Duration_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_delay);

                Program.Project.Undo.Add($"Delay Duration Changed to {r}{Duration.Unit}", () => {
                    ((Delay)Track.TraversePath(path)).Time.Free = u;
                }, () => {
                    ((Delay)Track.TraversePath(path)).Time.Free = r;
                });
            }

            _delay.Time.Free = (int)value;
        }

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        private void Duration_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_delay);

                Program.Project.Undo.Add($"Delay Duration Switched to {(r? "Steps" : "Free")}", () => {
                    ((Delay)Track.TraversePath(path)).Time.Mode = u;
                }, () => {
                    ((Delay)Track.TraversePath(path)).Time.Mode = r;
                });
            }

            _delay.Time.Mode = value;
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        private void Duration_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_delay);

                Program.Project.Undo.Add($"Delay Duration Changed to {Length.Steps[r]}", () => {
                    ((Delay)Track.TraversePath(path)).Time.Length.Step = u;
                }, () => {
                    ((Delay)Track.TraversePath(path)).Time.Length.Step = r;
                });
            }
        }

        public void SetDurationStep(int duration) => Duration.DrawArcAuto();

        private void Gate_Changed(double value, double? old) {
            if (old != null && old != value) {
                decimal u = (decimal)(old.Value / 100);
                decimal r = (decimal)(value / 100);
                List<int> path = Track.GetPath(_delay);

                Program.Project.Undo.Add($"Delay Gate Changed to {value}{Gate.Unit}", () => {
                    ((Delay)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Delay)Track.TraversePath(path)).Gate = r;
                });
            }

            _delay.Gate = (decimal)(value / 100);
        }

        public void SetGate(decimal gate) => Gate.RawValue = (double)gate * 100;
    }
}
