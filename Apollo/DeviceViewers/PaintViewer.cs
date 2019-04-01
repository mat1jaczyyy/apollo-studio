using System;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class PaintViewer: UserControl {
        public static readonly string DeviceIdentifier = "paint";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Paint _paint;
        Ellipse color;
        Thumb mainThumb, hueThumb;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;
            
            color = this.Get<Ellipse>("Color");
            color.Fill = _paint.Color.ToBrush();

            mainThumb = this.Get<Thumb>("MainThumb");
            hueThumb = this.Get<Thumb>("HueThumb");
        }

        private void UpdateColor() {
            double hue = Canvas.GetLeft(hueThumb) * 360 / ((Canvas)hueThumb.Parent).Bounds.Width;
            double saturation = Canvas.GetLeft(mainThumb) / ((Canvas)mainThumb.Parent).Bounds.Width;
            double value = (1 - (Canvas.GetTop(mainThumb) / ((Canvas)mainThumb.Parent).Bounds.Height)) * 63;

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)      _paint.Color = new Color(v, t, p);
            else if (hi == 1) _paint.Color = new Color(q, v, p);
            else if (hi == 2) _paint.Color = new Color(p, v, t);
            else if (hi == 3) _paint.Color = new Color(p, q, v);
            else if (hi == 4) _paint.Color = new Color(t, p, v);
            else              _paint.Color = new Color(v, p, q);
        }

        private void MainThumbMove(object sender, VectorEventArgs e) {
            Thumb thumb = (Thumb)e.Source;
            Canvas Area = (Canvas)thumb.Parent;

            double x = Canvas.GetLeft(thumb) + e.Vector.X;
            double y = Canvas.GetTop(thumb) + e.Vector.Y;

            if (0 <= x && x <= Area.Bounds.Width) Canvas.SetLeft(thumb, x);
            if (0 <= y && y <= Area.Bounds.Height) Canvas.SetTop(thumb, y);

            UpdateColor();
        }

        private void HueThumbMove(object sender, VectorEventArgs e) {
            Thumb thumb = (Thumb)e.Source;
            Canvas Area = (Canvas)thumb.Parent;

            double x = Canvas.GetLeft(thumb) + e.Vector.X;
            if (0 <= x && x <= Area.Bounds.Width) Canvas.SetLeft(thumb, x);

            UpdateColor();
        }
    }
}
