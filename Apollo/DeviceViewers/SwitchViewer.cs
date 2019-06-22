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

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Switch _switch;
        Dial Page;

        public SwitchViewer(Switch pageswitch) {
            InitializeComponent();

            _switch = pageswitch;

            Page = this.Get<Dial>("Page");
            Page.RawValue = _switch.Page;
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _switch = null;

        private void Page_Changed(double value, double? old) {
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
    }
}
