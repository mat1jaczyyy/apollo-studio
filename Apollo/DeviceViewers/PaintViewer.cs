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
        
        Ellipse color;
        Canvas mainCanvas, hueCanvas;
        Thumb mainThumb, hueThumb;
        GradientStop mainColor;
        TextBox Hex;

        bool main_mouseHeld, hue_mouseHeld, hexValidation;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;
            
            color = this.Get<Ellipse>("Color");
            color.Fill = _paint.Color.ToBrush();

            mainCanvas = this.Get<Canvas>("MainCanvas");
            hueCanvas = this.Get<Canvas>("HueCanvas");

            mainCanvas.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            hueCanvas.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);

            mainThumb = this.Get<Thumb>("MainThumb");
            hueThumb = this.Get<Thumb>("HueThumb");

            mainColor = this.Get<GradientStop>("MainColor");

            hexValidation = true;
            Hex = this.Get<TextBox>("Hex");
            Hex.GetObservable(TextBox.TextProperty).Subscribe(Hex_Changed);
        }

        public void Bounds_Updated(Rect bounds) {
            if (!bounds.IsEmpty) InitCanvas();
        }

        private void InitCanvas() {
            double hueHeight = hueCanvas.Bounds.Height;
            double mainWidth = mainCanvas.Bounds.Width;
            double mainHeight = mainCanvas.Bounds.Height;

            if (hueHeight == 0 || mainWidth == 0 || mainHeight == 0) return;

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

            Canvas.SetTop(hueThumb, hue * hueHeight / 6);
            Canvas.SetLeft(mainThumb, saturation * mainWidth);
            Canvas.SetTop(mainThumb, (1 - max) * mainHeight);

            UpdateCanvas();

            Hex.Text = _paint.Color.ToHex();
        }

        private void UpdateColor() {
            double hue = Canvas.GetTop(hueThumb) * 6 / hueCanvas.Bounds.Height;
            double saturation = Canvas.GetLeft(mainThumb) / mainCanvas.Bounds.Width;
            double value = (1 - (Canvas.GetTop(mainThumb) / mainCanvas.Bounds.Height)) * 63;

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

            hexValidation = false;
            Hex.Text = _paint.Color.ToHex();
            hexValidation = true;
        }

        private void UpdateCanvas() {
            double hue = Canvas.GetTop(hueThumb) * 6 / hueCanvas.Bounds.Height;

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
            x = (x < 0)? 0 : x;
            x = (x > mainCanvas.Bounds.Width)? mainCanvas.Bounds.Width : x;

            double y = Canvas.GetTop(mainThumb) + e.Vector.Y;
            y = (y < 0)? 0 : y;
            y = (y > mainCanvas.Bounds.Height)? mainCanvas.Bounds.Height : y;

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
            double y = Canvas.GetTop(hueThumb) + e.Vector.Y;
            y = (y < 0)? 0 : y;
            y = y > hueCanvas.Bounds.Height? hueCanvas.Bounds.Height : y;

            Canvas.SetTop(hueThumb, y);

            UpdateColor();
            UpdateCanvas();
        }

        private void HueCanvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                hue_mouseHeld = true;
                e.Device.Capture(hueCanvas);

                Vector position = e.GetPosition(hueThumb);
                position = position.WithY(position.Y - hueThumb.Bounds.Height / 2);

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
                position = position.WithY(position.Y - hueThumb.Bounds.Height / 2);

                HueThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        private Action HexAction(string text) {
            Action update = () => { Hex.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };

            foreach (char i in text.Substring(1))
                if (!"0123456789ABCDEF".Contains(i))
                    return update + (() => { Hex.Text = _paint.Color.ToHex(); });

            if (text == "#") return () => {
                Hex.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush");
                Hex.Text = text;
            };

            if (text[0] != '#' || text.Length > 7) return update + (() => { Hex.Text = _paint.Color.ToHex(); });
            if (text.Length < 7) return () => { Hex.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };

            int r = Convert.ToInt32(text.Substring(1, 2), 16);
            int g = Convert.ToInt32(text.Substring(3, 2), 16);
            int b = Convert.ToInt32(text.Substring(5, 2), 16);

            r = (r > 63)? 63 : r;
            g = (g > 63)? 63 : g;
            b = (b > 63)? 63 : b;

            return update + (() => { 
                _paint.Color = new Color((byte)r, (byte)g, (byte)b);

                color.Fill = _paint.Color.ToBrush();
                InitCanvas();
            });
        }

        private void Hex_Changed(string text) {
            if (!hexValidation) return;
            
            if (text == null) return;
            if (text == "") text = "#";

            Dispatcher.UIThread.InvokeAsync(HexAction(text.ToUpper()));
        }
        
        private void Hex_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) this.Focus();
        }
    }
}
