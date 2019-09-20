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
    public class LayerFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "layerfilter";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Target = this.Get<Dial>("Target");
            Range = this.Get<Dial>("Range");
        }
        
        LayerFilter _filter;
        
        Dial Target, Range;

        public LayerFilterViewer() => new InvalidOperationException();

        public LayerFilterViewer(LayerFilter filter) {
            InitializeComponent();

            _filter = filter;
            
            Target.RawValue = _filter.Target;
            Range.RawValue = _filter.Range;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _filter = null;

        void Target_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_filter);

                Program.Project.Undo.Add($"Layer Target Changed to {r}{Target.Unit}", () => {
                    ((LayerFilter)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((LayerFilter)Track.TraversePath(path)).Target = r;
                });
            }

            _filter.Target = (int)value;
        }

        public void SetTarget(int value) => Target.RawValue = value;

        void Range_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_filter);

                Program.Project.Undo.Add($"Layer Filter Range Changed to {r}{Range.Unit}", () => {
                    ((LayerFilter)Track.TraversePath(path)).Range = u;
                }, () => {
                    ((LayerFilter)Track.TraversePath(path)).Range = r;
                });
            }

            _filter.Range = (int)value;
        }

        public void SetRange(int value) => Range.RawValue = value;
    }
}
