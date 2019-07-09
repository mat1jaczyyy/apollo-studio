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
            
            Page = this.Get<Dial>("Page");
            MultiReset = this.Get<CheckBox>("MultiReset");
        }
        
        Switch _switch;
        
        Dial Page;
        CheckBox MultiReset;

        public SwitchViewer(Switch pageswitch) {
            InitializeComponent();

            _switch = pageswitch;

            Page.RawValue = _switch.Page;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _switch = null;

        void Page_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_switch);

                Program.Project.Undo.Add($"Switch Page Changed to {r}{Page.Unit}", () => {
                    ((Switch)Track.TraversePath(path)).Page = u;
                }, () => {
                    ((Switch)Track.TraversePath(path)).Page = r;
                });
            }

            _switch.Page = (int)value;
        }

        public void SetPage(int value) => Page.RawValue = value;

        void MultiReset_Changed(object sender, EventArgs e) {
            bool value = MultiReset.IsChecked.Value;

            if (_switch.MultiReset != value) {
                bool u = _switch.MultiReset;
                bool r = value;
                List<int> path = Track.GetPath(_switch);

                Program.Project.Undo.Add($"Switch Multi Reset Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Switch)Track.TraversePath(path)).MultiReset = u;
                }, () => {
                    ((Switch)Track.TraversePath(path)).MultiReset = r;
                });

                _switch.MultiReset = value;
            }
        }

        public void SetMultiReset(bool value) => MultiReset.IsChecked = value;
    }
}
