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
        
        Ellipse color;
        GradientStop mainColor;
        Thumb mainThumb, hueThumb;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;
            
            color = this.Get<Ellipse>("Color");
            color.Fill = _paint.Color.ToBrush();

            mainThumb = this.Get<Thumb>("MainThumb");
            hueThumb = this.Get<Thumb>("HueThumb");

            mainColor = this.Get<GradientStop>("MainColor");
        }

        private void InitCanvas(object sender, EventArgs e) {
            double hueWidth = ((Canvas)hueThumb.Parent).Bounds.Width;
            double mainWidth = ((Canvas)mainThumb.Parent).Bounds.Width;
            double mainHeight = ((Canvas)mainThumb.Parent).Bounds.Height;

            if (hueWidth == 0 || mainWidth == 0 || mainHeight == 0) return;

            double r = _paint.Color.Red / 63.0;
            double g = _paint.Color.Green / 63.0;
            double b = _paint.Color.Blue / 63.0;
            double[] color = new double[] {r, g, b};

            double min = color.Min();
            double max = color.Max();

            double hue = 0;
            if (min != max) {
                double diff = max - min;

                if (max == r) {
                    hue = (g - b) / diff;
                } else if (max == g) {
                    hue = (b - r) / diff + 2.0;
                } else if (max == b) {
                    hue = (r - g) / diff + 4.0;
                }
                if (hue < 0) hue += 6.0;
            }

            double saturation = 0;
            if (max != 0) saturation = 1 - (min / max);

            Canvas.SetLeft(hueThumb, hue * ((Canvas)hueThumb.Parent).Bounds.Width / 6);
            Canvas.SetLeft(mainThumb, saturation * ((Canvas)mainThumb.Parent).Bounds.Width);
            Canvas.SetTop(mainThumb, (1 - max) * ((Canvas)mainThumb.Parent).Bounds.Height);

            UpdateCanvas();

            ((Canvas)sender).LayoutUpdated -= InitCanvas;
        }

        private void UpdateColor() {
            double hue = Canvas.GetLeft(hueThumb) * 6 / ((Canvas)hueThumb.Parent).Bounds.Width;
            double saturation = Canvas.GetLeft(mainThumb) / ((Canvas)mainThumb.Parent).Bounds.Width;
            double value = (1 - (Canvas.GetTop(mainThumb) / ((Canvas)mainThumb.Parent).Bounds.Height)) * 63;

            int hi = Convert.ToInt32(Math.Floor(hue)) % 6;
            double f = hue - Math.Floor(hue);

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

            color.Fill = _paint.Color.ToBrush();
        }

        private void UpdateCanvas() {
            double hue = Canvas.GetLeft(hueThumb) * 6 / ((Canvas)hueThumb.Parent).Bounds.Width;

            int hi = Convert.ToInt32(Math.Floor(hue)) % 6;
            double f = hue - Math.Floor(hue);

            byte v = 255;
            byte p = 0;
            byte q = Convert.ToByte(255 * (1 - f));
            byte t = Convert.ToByte(255 * f);

            if (hi == 0)      mainColor.Color = new AvaloniaColor(255, v, t, p);
            else if (hi == 1) mainColor.Color = new AvaloniaColor(255, q, v, p);
            else if (hi == 2) mainColor.Color = new AvaloniaColor(255, p, v, t);
            else if (hi == 3) mainColor.Color = new AvaloniaColor(255, p, q, v);
            else if (hi == 4) mainColor.Color = new AvaloniaColor(255, t, p, v);
            else              mainColor.Color = new AvaloniaColor(255, v, p, q);
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
            UpdateCanvas();
        }
    }
}
