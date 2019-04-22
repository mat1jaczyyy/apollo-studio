using System;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class MoveDial: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void MoveDialChangedEventHandler(int x, int y);
        public event MoveDialChangedEventHandler Changed;

        Canvas PlaneCanvas;
        Rectangle XRect, YRect, Point;

        private int _x = 0;
        public int X {
            get => _x;
            set {
                value = Math.Max(-9, Math.Min(9, value));
                if (value != _x) {
                    _x = value;
                    DrawX();
                    DrawPoint();
                    Changed?.Invoke(_x, _y);
                }
            }
        }

        private int _y = 0;
        public int Y {
            get => _y;
            set {
                value = Math.Max(-9, Math.Min(9, value));
                if (value != _y) {
                    _y = value;
                    DrawY();
                    DrawPoint();
                    Changed?.Invoke(_x, _y);
                }
            }
        }

        private string _title = "Dial";
        public string Title {
            get => _title;
            set {
                this.Get<TextBlock>("Title").Text = _title = value;
            }
        }

        private string ValueString => $"({_x}, {_y})";

        private void DrawPoint() {
            Canvas.SetLeft(Point, 18 + 2 * _x);
            Canvas.SetTop(Point, 18 - 2 * _y);

            this.Get<TextBlock>("Display").Text = ValueString;
        }

        private void DrawX() {
            XRect.Width = Math.Abs(2 * _x) + 2;
            Canvas.SetLeft(XRect, (_x > 0)? 18 : 20 - XRect.Width);
        }

        private void DrawY() {
            YRect.Height = Math.Abs(2 * _y) + 2;
            Canvas.SetTop(YRect, (_y < 0)? 18 : 20 - YRect.Height);
        }

        public MoveDial() {
            InitializeComponent();

            PlaneCanvas = this.Get<Canvas>("PlaneCanvas");
            XRect = this.Get<Rectangle>("XRect");
            YRect = this.Get<Rectangle>("YRect");
            Point = this.Get<Rectangle>("Point");

            DrawX();
            DrawY();
            DrawPoint();
        }

        private bool mouseHeld = false;
        private double lastX, lastY;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;
                e.Device.Capture(PlaneCanvas);

                lastX = e.GetPosition(PlaneCanvas).X;
                lastY = e.GetPosition(PlaneCanvas).Y;
                PlaneCanvas.Cursor = new Cursor(StandardCursorType.SizeAll);
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = false;
                e.Device.Capture(null);

                PlaneCanvas.Cursor = new Cursor(StandardCursorType.Hand);
            }
        }

        private void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                double x = e.GetPosition(PlaneCanvas).X;
                double y = e.GetPosition(PlaneCanvas).Y;

                if (Math.Abs(x - lastX) >= 4) {
                    X += (int)((x - lastX) / 4);
                    lastX = x;
                }

                if (Math.Abs(y - lastY) >= 4) {
                    Y -= (int)((y - lastY) / 4);
                    lastY = y;
                }
            }
        }
    }
}
