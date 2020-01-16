using System;
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

        public DelayViewer() => new InvalidOperationException();

        public DelayViewer(Delay delay) {
            InitializeComponent();

            _delay = delay;
            
            Duration.UsingSteps = _delay.Time.Mode;
            Duration.Length = _delay.Time.Length;
            Duration.RawValue = _delay.Time.Free;

            Gate.RawValue = _delay.Gate * 100;
        }

        void Duration_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;

                Program.Project.Undo.AddAndExecute(new Delay.DurationUndoEntry(_delay, Duration.Unit, u, r));
            }
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _delay = null;

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        void Duration_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;

                Program.Project.Undo.AddAndExecute(new Delay.DurationModeUndoEntry(_delay, u, r));
            }
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        void Duration_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;

                Program.Project.Undo.AddAndExecute(new Delay.DurationStepUndoEntry(_delay, u, r));
            }
        }

        public void SetDurationStep(Length duration) => Duration.Length = duration;

        void Gate_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;

                Program.Project.Undo.AddAndExecute(new Delay.GateUndoEntry(_delay, u, r));
            }
        }

        public void SetGate(double gate) => Gate.RawValue = gate * 100;
    }
}
