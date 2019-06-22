using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class PaintViewer: UserControl {
        public static readonly string DeviceIdentifier = "paint";

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Picker = this.Get<ColorPicker>("Picker");
        }
        
        Paint _paint;
        ColorPicker Picker;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;

            Picker.SetColor(_paint.Color);
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _paint = null;
        
        private void Color_Changed(Color color, Color old) {
            if (old != null) {
                Color u = old.Clone();
                Color r = color.Clone();
                List<int> path = Track.GetPath(_paint);

                Program.Project.Undo.Add($"Paint Color Changed to {r.ToHex()}", () => {
                    ((Paint)Track.TraversePath(path)).Color = u.Clone();
                }, () => {
                    ((Paint)Track.TraversePath(path)).Color = r.Clone();
                });
            }

            _paint.Color = color;
        }

        public void Set(Color color) => Picker.SetColor(color);
    }
}
