using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class LayerViewer: UserControl {
        public static readonly string DeviceIdentifier = "layer";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Layer _layer;
        
        Dial Target;
        ComboBox BlendingMode;

        public LayerViewer(Layer layer) {
            InitializeComponent();

            _layer = layer;
            
            Target = this.Get<Dial>("Target");
            Target.RawValue = _layer.Target;

            BlendingMode = this.Get<ComboBox>("BlendingMode");
            BlendingMode.SelectedItem = _layer.Mode;
        }

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
            string selected = (string)BlendingMode.SelectedItem;

            if (_layer.Mode != selected) {
                string u = _layer.Mode;
                string r = selected;
                List<int> path = Track.GetPath(_layer);

                Program.Project.Undo.Add($"Layer Blending Changed to {selected}", () => {
                    ((Layer)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Layer)Track.TraversePath(path)).Mode = r;
                });

                _layer.Mode = selected;
            }
        }

        public void SetMode(string mode) => BlendingMode.SelectedItem = mode;
    }
}
