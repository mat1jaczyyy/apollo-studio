using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class OutputViewer: UserControl {
        public static readonly string DeviceIdentifier = "output";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Output _output;
        Dial Target;

        private void Update_Target(int value) {
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

        private void Target_Changed(double value) => _output.Target = (int)value - 1;
    }
}
