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

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Target = this.Get<Dial>("Target");
            BlendingMode = this.Get<ComboBox>("BlendingMode");
        }
        
        Layer _layer;
        
        Dial Target;
        ComboBox BlendingMode;

        public LayerViewer(Layer layer) {
            InitializeComponent();

            _layer = layer;
            
            Target.RawValue = _layer.Target;
            BlendingMode.SelectedIndex = (int)_layer.BlendingMode;
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _layer = null;

        private void Target_Changed(double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_layer);

                Program.Project.Undo.Add($"Layer Target Changed to {r}{Target.Unit}", () => {
                    ((Layer)Track.TraversePath(path)).Target = u;
                }, () => {
                    ((Layer)Track.TraversePath(path)).Target = r;
                });
            }

            _layer.Target = (int)value;
        }

        public void SetTarget(int value) => Target.RawValue = value;

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            BlendingType selected = (BlendingType)BlendingMode.SelectedIndex;

            if (_layer.BlendingMode != selected) {
                BlendingType u = _layer.BlendingMode;
                BlendingType r = selected;
                List<int> path = Track.GetPath(_layer);

                Program.Project.Undo.Add($"Layer Blending Changed to {r}", () => {
                    ((Layer)Track.TraversePath(path)).BlendingMode = u;
                }, () => {
                    ((Layer)Track.TraversePath(path)).BlendingMode = r;
                });

                _layer.BlendingMode = selected;
            }
        }

        public void SetMode(BlendingType mode) => BlendingMode.SelectedIndex = (int)mode;
    }
}
