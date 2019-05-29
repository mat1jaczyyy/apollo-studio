using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class PageSwitchViewer: UserControl {
        public static readonly string DeviceIdentifier = "pageswitch";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        PageSwitch _pageswitch;
        Dial Target;

        public PageSwitchViewer(PageSwitch pageswitch) {
            InitializeComponent();

            _pageswitch = pageswitch;

            Target = this.Get<Dial>("Target");
            Target.RawValue = _pageswitch.Target;
        }

        private void Target_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_pageswitch);

                Program.Project.Undo.Add($"PageSwitch Target Changed to {r}{Target.Unit}", () => {
                    ((PageSwitch)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((PageSwitch)Track.TraversePath(path)).Target = r;
                });
            }

            _pageswitch.Target = (int)value;
        }

        public void SetTarget(int value) => Target.RawValue = value;
    }
}
