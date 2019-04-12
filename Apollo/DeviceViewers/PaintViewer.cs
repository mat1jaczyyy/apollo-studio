using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaColor = Avalonia.Media.Color;
using GradientStop = Avalonia.Media.GradientStop;
using IBrush = Avalonia.Media.IBrush;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Components;
using Apollo.Devices;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class PaintViewer: UserControl {
        public static readonly string DeviceIdentifier = "paint";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Paint _paint;
        ColorPicker Picker;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;

            Picker = this.Get<ColorPicker>("Picker");
            Picker.SetColor(_paint.Color);
        }
        
        private void Color_Changed(Color color) => _paint.Color = color;
    }
}
