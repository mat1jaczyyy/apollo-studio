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
    public class ChokeViewer: UserControl, ISelectParentViewer {
        public static readonly string DeviceIdentifier = "choke";

        public int? IExpanded {
            get => 0;
        }

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
        
        public void Expand(int? index) {}

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

        public void Copy(int left, int right, bool cut = false) {}
        public void Paste(int right) {}
        public void Duplicate(int left, int right) {}
        public void Delete(int left, int right) {}
        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        public void Mute(int left, int right) {}
        public void Rename(int left, int right) {}
        public void Export(int left, int right) {}
        public void Import(int right) {}
    }
}
