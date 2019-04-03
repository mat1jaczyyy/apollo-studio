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
    public class PaintViewer: UserControl, IObserver<Rect> {
        public static readonly string DeviceIdentifier = "paint";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Paint _paint;
        
        Ellipse color;
        Canvas mainCanvas, hueCanvas;
        Thumb mainThumb, hueThumb;
        GradientStop mainColor;

        bool main_mouseHeld, hue_mouseHeld;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;
            
            color = this.Get<Ellipse>("Color");
            color.Fill = _paint.Color.ToBrush();

            mainCanvas = this.Get<Canvas>("MainCanvas");
            hueCanvas = this.Get<Canvas>("HueCanvas");

            mainCanvas.GetObservable(Visual.BoundsProperty).Subscribe(this);
            hueCanvas.GetObservable(Visual.BoundsProperty).Subscribe(this);

            mainThumb = this.Get<Thumb>("MainThumb");
            hueThumb = this.Get<Thumb>("HueThumb");

            mainColor = this.Get<GradientStop>("MainColor");
        }

        public void OnCompleted() {}
        public void OnError(Exception e) {}
        public void OnNext(Rect bounds) {
            if (!bounds.IsEmpty) InitCanvas();
        }

        private void InitCanvas() {
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

        private void MainThumb_Move(object sender, VectorEventArgs e) {
            double x = Canvas.GetLeft(mainThumb) + e.Vector.X;
            x = x < 0? 0 : x;
            x = x > mainCanvas.Bounds.Width? mainCanvas.Bounds.Width : x;

            double y = Canvas.GetTop(mainThumb) + e.Vector.Y;
            y = y < 0? 0 : y;
            y = y > mainCanvas.Bounds.Height? mainCanvas.Bounds.Height : y;

            Canvas.SetLeft(mainThumb, x);
            Canvas.SetTop(mainThumb, y);

            UpdateColor();
        }

        private void MainCanvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                main_mouseHeld = true;
                e.Device.Capture(mainCanvas);

                Vector position = e.GetPosition(mainThumb);
                position = position.WithX(position.X - mainThumb.Bounds.Width / 2)
                                   .WithY(position.Y - mainThumb.Bounds.Height / 2);

                MainThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        private void MainCanvas_MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                main_mouseHeld = false;
                e.Device.Capture(null);
            }
        }

        private void MainCanvas_MouseMove(object sender, PointerEventArgs e) {
            if (main_mouseHeld) {
                Vector position = e.GetPosition(mainThumb);
                position = position.WithX(position.X - mainThumb.Bounds.Width / 2)
                                   .WithY(position.Y - mainThumb.Bounds.Height / 2);

                MainThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        private void HueThumb_Move(object sender, VectorEventArgs e) {
            double x = Canvas.GetLeft(hueThumb) + e.Vector.X;
            x = x < 0? 0 : x;
            x = x > hueCanvas.Bounds.Width? hueCanvas.Bounds.Width : x;

            Canvas.SetLeft(hueThumb, x);

            UpdateColor();
            UpdateCanvas();
        }

        private void HueCanvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                hue_mouseHeld = true;
                e.Device.Capture(hueCanvas);

                Vector position = e.GetPosition(hueThumb);
                position = position.WithX(position.X - hueThumb.Bounds.Width / 2)
                                   .WithY(position.Y - hueThumb.Bounds.Height / 2);

                HueThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        private void HueCanvas_MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                hue_mouseHeld = false;
                e.Device.Capture(null);
            }
        }

        private void HueCanvas_MouseMove(object sender, PointerEventArgs e) {
            if (hue_mouseHeld) {
                Vector position = e.GetPosition(hueThumb);
                position = position.WithX(position.X - hueThumb.Bounds.Width / 2);

                HueThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }
    }
}
