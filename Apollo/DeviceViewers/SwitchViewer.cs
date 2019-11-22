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
            
            Target = this.Get<Dial>("Target");
            Value = this.Get<Dial>("Value");
        }
        
        Switch _switch;
        
        Dial Target;
        Dial Value;

        public SwitchViewer() => new InvalidOperationException();

        public SwitchViewer(Switch macroswitch) {
            InitializeComponent();

            _switch = macroswitch;

            Target.RawValue = _switch.Target;
            Value.RawValue = _switch.Value;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _switch = null;

        void Target_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_switch);

                Program.Project.Undo.Add($"Switch Target Changed to {r}{Target.Unit}", () => {
                    ((Switch)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((Switch)Track.TraversePath(path)).Target = r;
                });
            }

            _switch.Target = (int)value;
        }
       
        public void SetTarget(int target) => Target.RawValue = target;
        
        void Value_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_switch);

                Program.Project.Undo.Add($"Switch Value Changed to {r}{Target.Unit}", () => {
                    ((Switch)Track.TraversePath(path)).Value = u;
                }, () => {
                    ((Switch)Track.TraversePath(path)).Value = r;
                });
            }

            _switch.Value = (int)value;
        }
        
        public void SetValue(int value) => Value.RawValue = value;
    }
}
