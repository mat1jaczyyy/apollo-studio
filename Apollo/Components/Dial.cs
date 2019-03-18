using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class Dial: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        private const double radius = 18, stroke = 7;
        private const double strokeHalf = stroke / 2;

        private const double angle_start = 4 * Math.PI / 3;
        private const double angle_end = -1 * Math.PI / 3;

        private double _value = 0.5;
        public double Value {
            get {
                return _value;
            }
            set {
                _value = Math.Max(0, Math.Min(1, value));
                DrawArc(this.Get<Path>("Arc"), _value);
            }
        }

        private string _title = "Dial";
        public string Title {
            get {
                return _title;
            }
            set {
                this.Get<TextBlock>("Title").Text = _title = value;
            }
        }

        private void DrawArc(Path Arc, double value) {
            double x_start = radius * (Math.Cos(angle_start) + 1) + strokeHalf;
            double y_start = radius * (-Math.Sin(angle_start) + 1) + strokeHalf;
            
            double angle_point = angle_start - Math.Abs(angle_end - angle_start) * value;

            double x_end = radius * (Math.Cos(angle_point) + 1) + strokeHalf;
            double y_end = radius * (-Math.Sin(angle_point) + 1) + strokeHalf;

            double angle = (angle_start - angle_point) / Math.PI * 180;

            int large = Convert.ToInt32(angle > 180);

            Arc.StrokeThickness = stroke;
            Arc.Data = Geometry.Parse($"M {x_start},{y_start} A {radius},{radius} {angle} {large} 1 {x_end},{y_end}");
        }

        public Dial() {
            InitializeComponent();

            this.Get<TextBlock>("Title").Text = _title;

            DrawArc(this.Get<Path>("ArcBase"), 1);
            DrawArc(this.Get<Path>("Arc"), _value);
        }

        private bool mouseHeld = false;
        private double lastY;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            mouseHeld = true;
            Canvas ArcCanvas = this.Get<Canvas>("ArcCanvas");

            lastY = e.GetPosition(ArcCanvas).Y;
            ArcCanvas.Cursor = new Cursor(StandardCursorType.No);
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            mouseHeld = false;
            this.Get<Canvas>("ArcCanvas").Cursor = new Cursor(StandardCursorType.Arrow);
        }

        private void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                Canvas ArcCanvas = this.Get<Canvas>("ArcCanvas");
                double Y = e.GetPosition(ArcCanvas).Y;
                Value += (lastY - Y) / 200;
                lastY = Y;
            }
        }
    }
}
