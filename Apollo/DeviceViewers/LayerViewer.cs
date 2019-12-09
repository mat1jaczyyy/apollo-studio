using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;

namespace Apollo.DeviceViewers {
    public class LayerViewer: UserControl {
        public static readonly string DeviceIdentifier = "layer";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Target = this.Get<Dial>("Target");
            BlendingMode = this.Get<ComboBox>("BlendingMode");
            Range = this.Get<Dial>("Range");
        }
        
        Layer _layer;
        
        Dial Target, Range;
        ComboBox BlendingMode;

        public LayerViewer() => new InvalidOperationException();

        public LayerViewer(Layer layer) {
            InitializeComponent();

            _layer = layer;
            
            Target.RawValue = _layer.Target;
            SetMode(_layer.BlendingMode);
            Range.RawValue = _layer.Range;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _layer = null;

        void Target_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_layer);

                Program.Project.Undo.Add($"Layer Target Changed to {r}{Target.Unit}", () => {
                    Track.TraversePath<Layer>(path).Target = u;
                }, () => {
                    Track.TraversePath<Layer>(path).Target = r;
                });
            }

            _layer.Target = (int)value;
        }

        public void SetTarget(int value) => Target.RawValue = value;

        void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            BlendingType selected = (BlendingType)BlendingMode.SelectedIndex;

            if (_layer.BlendingMode != selected) {
                BlendingType u = _layer.BlendingMode;
                BlendingType r = selected;
                List<int> path = Track.GetPath(_layer);

                Program.Project.Undo.Add($"Layer Blending Changed to {r}", () => {
                    Track.TraversePath<Layer>(path).BlendingMode = u;
                }, () => {
                    Track.TraversePath<Layer>(path).BlendingMode = r;
                });

                _layer.BlendingMode = selected;
            }
        }

        public void SetMode(BlendingType mode) => Range.Enabled = (BlendingMode.SelectedIndex = (int)mode) > 0;

        void Range_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_layer);

                Program.Project.Undo.Add($"Layer Range Changed to {r}{Range.Unit}", () => {
                    Track.TraversePath<Layer>(path).Range = u;
                }, () => {
                    Track.TraversePath<Layer>(path).Range = r;
                });
            }

            _layer.Range = (int)value;
        }

        public void SetRange(int value) => Range.RawValue = value;
    }
}
