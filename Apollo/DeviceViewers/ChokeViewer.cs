using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.DeviceViewers {
    public class ChokeViewer: UserControl {
        public static readonly string DeviceIdentifier = "choke";

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Target = this.Get<Dial>("Target");
        }
        
        Choke _choke;

        Dial Target;

        public ChokeViewer(Choke choke, DeviceViewer parent) {
            InitializeComponent();

            _choke = choke;

            Target.RawValue = _choke.Target;

            parent.Border.CornerRadius = new CornerRadius(5, 0, 0, 5);
            parent.Header.CornerRadius = new CornerRadius(5, 0, 0, 0);

            parent.Root.Children.Insert(1, new ChainViewer(choke.Chain, true));
            parent.Root.Children.Insert(2, new DeviceTail(_choke, parent));
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _choke = null;

        private void Target_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_choke);

                Program.Project.Undo.Add($"Choke Target Changed to {r}{Target.Unit}", () => {
                    ((Choke)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((Choke)Track.TraversePath(path)).Target = r;
                });
            }

            _choke.Target = (int)value;
        }

        public void SetTarget(int value) => Target.RawValue = value;
    }
}
