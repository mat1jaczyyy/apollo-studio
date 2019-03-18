using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
                _value = value;
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
    }
}
