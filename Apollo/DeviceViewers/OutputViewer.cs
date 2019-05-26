using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class OutputViewer: UserControl {
        public static readonly string DeviceIdentifier = "output";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Output _output;
        Dial Target;

        public void Update_Target(int value) {
            Target.RawValue = value + 1;
        }

        private void Update_Maximum(int value) {
            Target.Enabled = (value != 1);
            if (Target.Enabled) Target.Maximum = value;
        } 

        public OutputViewer(Output output) {
            InitializeComponent();
            
            _output = output;
            _output.TargetChanged += Update_Target;
            Program.Project.TrackCountChanged += Update_Maximum;

            Target = this.Get<Dial>("Target");
            Update_Maximum(Program.Project.Tracks.Count);
            Update_Target(_output.Target);
        }

        private void Target_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value - 1;
                int r = (int)value - 1;
                List<int> path = Track.GetPath(_output);

                Program.Project.Undo.Add($"Output Target Changed", () => {
                    ((Output)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((Output)Track.TraversePath(path)).Target = r;
                });
            }

            _output.Target = (int)value - 1;
        }
    }
}
