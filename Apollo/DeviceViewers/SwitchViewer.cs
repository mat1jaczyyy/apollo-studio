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
    public class SwitchViewer: UserControl {
        public static readonly string DeviceIdentifier = "switch";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Macro = this.Get<Dial>("Macro");
        }
        
        Switch _switch;
        
        Dial Macro;

        public SwitchViewer() => new InvalidOperationException();

        public SwitchViewer(Switch macroswitch) {
            InitializeComponent();

            _switch = macroswitch;

            Macro.RawValue = _switch.Macro;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _switch = null;

        void Macro_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_switch);

                Program.Project.Undo.Add($"Switch Macro Changed to {r}{Macro.Unit}", () => {
                    ((Switch)Track.TraversePath(path)).Macro = u;
                }, () => {
                    ((Switch)Track.TraversePath(path)).Macro = r;
                });
            }

            _switch.Macro = (int)value;
        }

        public void SetMacro(int value) => Macro.RawValue = value;
    }
}
