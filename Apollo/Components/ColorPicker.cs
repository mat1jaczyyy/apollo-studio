using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaColor = Avalonia.Media.Color;
using LinearGradientBrush = Avalonia.Media.LinearGradientBrush;
using GradientStop = Avalonia.Media.GradientStop;
using IBrush = Avalonia.Media.IBrush;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Structures;

namespace Apollo.Components {
    public class ColorPicker: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Preview = this.Get<Ellipse>("Color");

            MainCanvas = this.Get<Canvas>("MainCanvas");
            HueCanvas = this.Get<Canvas>("HueCanvas");

            MainThumb = this.Get<Thumb>("MainThumb");
            HueThumb = this.Get<Thumb>("HueThumb");

            MainColor = ((LinearGradientBrush)this.Get<Grid>("MainColor").Background).GradientStops[1];
            
            Hex = this.Get<TextBox>("Hex");
        }
        
        public delegate void ColorChangedEventHandler(Color value, Color old);
        public event ColorChangedEventHandler ColorChanged;

        Color _color = new Color();
        public Color Color {
            get => _color;
            private set {
                _color = value;
                ColorChanged?.Invoke(_color, null);
            }
        }

        public void SetColor(Color color) {
            if (_color != color) {
                _color = color;
                Preview.Fill = Color.ToScreenBrush();
                InitCanvas();
            }
        }

        Ellipse Preview;
        Canvas MainCanvas, HueCanvas;
        Thumb MainThumb, HueThumb;
        GradientStop MainColor;
        TextBox Hex;

        bool main_mouseHeld, hue_mouseHeld, hexValidation;
        Color oldColor;

        public ColorPicker() {
            InitializeComponent();

            MainCanvas.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            HueCanvas.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            
            MainThumb.AddHandler(InputElement.PointerPressedEvent, Thumb_Down, RoutingStrategies.Tunnel);
            MainThumb.AddHandler(InputElement.PointerReleasedEvent, Thumb_Up, RoutingStrategies.Tunnel);

            HueThumb.AddHandler(InputElement.PointerPressedEvent, Thumb_Down, RoutingStrategies.Tunnel);
            HueThumb.AddHandler(InputElement.PointerReleasedEvent, Thumb_Up, RoutingStrategies.Tunnel);

            hexValidation = true;
            Hex.GetObservable(TextBox.TextProperty).Subscribe(Hex_Changed);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            ColorChanged = null;
            
            MainThumb.RemoveHandler(InputElement.PointerPressedEvent, Thumb_Down);
            MainThumb.RemoveHandler(InputElement.PointerReleasedEvent, Thumb_Up);

            HueThumb.RemoveHandler(InputElement.PointerPressedEvent, Thumb_Down);
            HueThumb.RemoveHandler(InputElement.PointerReleasedEvent, Thumb_Up);
        }

        public void Bounds_Updated(Rect bounds) {
            if (!bounds.IsEmpty) InitCanvas();
        }

        void InitCanvas() {
            double hueHeight = HueCanvas.Bounds.Height;
            double mainWidth = MainCanvas.Bounds.Width;
            double mainHeight = MainCanvas.Bounds.Height;

            if (hueHeight == 0 || mainWidth == 0 || mainHeight == 0) return;

            (double hue, double saturation, double value) = Color.ToHSV();

            Canvas.SetTop(HueThumb, hue * hueHeight / 360);
            Canvas.SetLeft(MainThumb, saturation * mainWidth);
            Canvas.SetTop(MainThumb, (1 - value) * mainHeight);

            UpdateCanvas();
            UpdateText();
        }

        void UpdateText() {
            hexValidation = false;
            Hex.Text = Color.ToHex();
            hexValidation = true;
        }

        void UpdateColor() {
            double hue = Canvas.GetTop(HueThumb) * 360 / HueCanvas.Bounds.Height;
            double saturation = Canvas.GetLeft(MainThumb) / MainCanvas.Bounds.Width;
            double value = (1 - (Canvas.GetTop(MainThumb) / MainCanvas.Bounds.Height));

            Color = Color.FromHSV(hue, saturation, value);

            Preview.Fill = Color.ToScreenBrush();

            UpdateText();
        }

        void UpdateCanvas() {
            double hue = Canvas.GetTop(HueThumb) * 6 / HueCanvas.Bounds.Height;

            int hi = Convert.ToInt32(Math.Floor(hue)) % 6;
            double f = hue - Math.Floor(hue);

            byte v = 255;
            byte p = 0;
            byte q = Convert.ToByte(255 * (1 - f));
            byte t = Convert.ToByte(255 * f);

            if (hi == 0)      MainColor.Color = new AvaloniaColor(255, v, t, p);
            else if (hi == 1) MainColor.Color = new AvaloniaColor(255, q, v, p);
            else if (hi == 2) MainColor.Color = new AvaloniaColor(255, p, v, t);
            else if (hi == 3) MainColor.Color = new AvaloniaColor(255, p, q, v);
            else if (hi == 4) MainColor.Color = new AvaloniaColor(255, t, p, v);
            else              MainColor.Color = new AvaloniaColor(255, v, p, q);
        }

        void Thumb_Down(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed)
                oldColor = Color.Clone();
        }

        void Thumb_Up(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased && oldColor != Color)
                ColorChanged?.Invoke(Color, oldColor);
        }

        void MainThumb_Move(object sender, VectorEventArgs e) {
            double x = Canvas.GetLeft(MainThumb) + e.Vector.X;
            x = (x < 0)? 0 : x;
            x = (x > MainCanvas.Bounds.Width)? MainCanvas.Bounds.Width : x;

            double y = Canvas.GetTop(MainThumb) + e.Vector.Y;
            y = (y < 0)? 0 : y;
            y = (y > MainCanvas.Bounds.Height)? MainCanvas.Bounds.Height : y;

            Canvas.SetLeft(MainThumb, x);
            Canvas.SetTop(MainThumb, y);

            UpdateColor();
        }

        void MainCanvas_MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) {
                main_mouseHeld = true;
                e.Pointer.Capture(MainCanvas);

                oldColor = Color.Clone();

                Vector position = e.GetPosition(MainThumb);
                position = position.WithX(position.X - MainThumb.Bounds.Width / 2)
                                   .WithY(position.Y - MainThumb.Bounds.Height / 2);

                MainThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        void MainCanvas_MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (main_mouseHeld && MouseButton == PointerUpdateKind.LeftButtonReleased) {
                main_mouseHeld = false;
                e.Pointer.Capture(null);

                if (oldColor != Color)
                    ColorChanged?.Invoke(Color, oldColor);
            }
        }

        void MainCanvas_MouseMove(object sender, PointerEventArgs e) {
            if (main_mouseHeld) {
                Vector position = e.GetPosition(MainThumb);
                position = position.WithX(position.X - MainThumb.Bounds.Width / 2)
                                   .WithY(position.Y - MainThumb.Bounds.Height / 2);

                MainThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        void HueThumb_Move(object sender, VectorEventArgs e) {
            double y = Canvas.GetTop(HueThumb) + e.Vector.Y;
            y = (y < 0)? 0 : y;
            y = y > HueCanvas.Bounds.Height? HueCanvas.Bounds.Height : y;

            Canvas.SetTop(HueThumb, y);

            UpdateColor();
            UpdateCanvas();
        }

        void HueCanvas_MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) {
                hue_mouseHeld = true;
                e.Pointer.Capture(HueCanvas);

                oldColor = Color.Clone();

                Vector position = e.GetPosition(HueThumb);
                position = position.WithY(position.Y - HueThumb.Bounds.Height / 2);

                HueThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        void HueCanvas_MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (hue_mouseHeld && MouseButton == PointerUpdateKind.LeftButtonReleased) {
                hue_mouseHeld = false;
                e.Pointer.Capture(null);
                
                if (oldColor != Color)
                    ColorChanged?.Invoke(Color, oldColor);
            }
        }

        void HueCanvas_MouseMove(object sender, PointerEventArgs e) {
            if (hue_mouseHeld) {
                Vector position = e.GetPosition(HueThumb);
                position = position.WithY(position.Y - HueThumb.Bounds.Height / 2);

                HueThumb_Move(null, new VectorEventArgs() { Vector = position });
            }
        }

        bool Hex_Dirty = false;

        Action HexAction(string text) {
            Action update = () => { Hex.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };

            foreach (char i in text.Substring(1))
                if (!"0123456789ABCDEF".Contains(i))
                    return update + (() => { UpdateText(); });

            if (text == "#") return () => {
                Hex.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush");
                Hex.Text = text;
            };

            if (text[0] != '#' || text.Length > 7) return update + (() => { UpdateText(); });
            if (text.Length < 7) return () => { Hex.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };

            int r = Convert.ToInt32(text.Substring(1, 2), 16);
            int g = Convert.ToInt32(text.Substring(3, 2), 16);
            int b = Convert.ToInt32(text.Substring(5, 2), 16);

            r = (r > 63)? 63 : r;
            g = (g > 63)? 63 : g;
            b = (b > 63)? 63 : b;

            if (!Hex_Dirty) {
                oldColor = Color.Clone();
                Hex_Dirty = true;
            }

            return update + (() => { 
                Color = new Color((byte)r, (byte)g, (byte)b);

                Preview.Fill = Color.ToScreenBrush();
                InitCanvas();
            });
        }

        void Hex_Changed(string text) {
            if (!hexValidation) return;
            
            if (text == null) return;
            if (text == "") text = "#";

            Dispatcher.UIThread.InvokeAsync(HexAction(text.ToUpper()));
        }
        
        void Hex_KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return)
                this.Focus();
            
            e.Key = Key.None;
        }
        
        void Hex_KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        void Hex_Unfocus(object sender, RoutedEventArgs e) {
            if (oldColor != Color)
                ColorChanged?.Invoke(Color, oldColor);

            Hex_Dirty = false;
        }
    }
}
