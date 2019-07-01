using System.Collections.Generic;

using Avalonia;
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

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Duration = this.Get<Dial>("Duration");
            Gate = this.Get<Dial>("Gate");
        }
        
        Delay _delay;
        Dial Duration, Gate;

        public DelayViewer(Delay delay) {
            InitializeComponent();

            _delay = delay;
            
            Duration.UsingSteps = _delay.Time.Mode;
            Duration.Length = _delay.Time.Length;
            Duration.RawValue = _delay.Time.Free;

            Gate.RawValue = (double)_delay.Gate * 100;
        }

        void Duration_Changed(double value, double? old) {
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

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _delay = null;

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        void Duration_ModeChanged(bool value, bool? old) {
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

        void Duration_StepChanged(int value, int? old) {
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

        public void SetDurationStep(Length duration) => Duration.Length = duration;

        void Gate_Changed(double value, double? old) {
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
