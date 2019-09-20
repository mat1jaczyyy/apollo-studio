using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class OutputViewer: UserControl {
        public static readonly string DeviceIdentifier = "output";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Target = this.Get<Dial>("Target");
        }
        
        Output _output;
        Dial Target;

        void Update_Maximum(int value) {
            Target.Enabled = (value != 1);
            if (Target.Enabled) Target.Maximum = value;
        }

        public OutputViewer() => new InvalidOperationException();

        public OutputViewer(Output output) {
            InitializeComponent();
            
            _output = output;
            Program.Project.TrackCountChanged += Update_Maximum;

            Update_Maximum(Program.Project.Tracks.Count);
            SetTarget(_output.Target);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Program.Project.TrackCountChanged -= Update_Maximum;

            _output = null;
        }

        void Target_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value - 1;
                int r = (int)value - 1;
                List<int> path = Track.GetPath(_output);

                Program.Project.Undo.Add($"Output Target Changed to {r + 1}{Target.Unit}", () => {
                    ((Output)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((Output)Track.TraversePath(path)).Target = r;
                });
            }

            _output.Target = (int)value - 1;
        }

        public void SetTarget(int value) => Target.RawValue = value + 1;
    }
}
