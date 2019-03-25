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

        public delegate void DialChangedEventHandler(double NewValue);
        public event DialChangedEventHandler Changed;

        private const double radius = 18, stroke = 7;
        private const double strokeHalf = stroke / 2;

        private const double angle_start = 4 * Math.PI / 3;
        private const double angle_end = -1 * Math.PI / 3;

        private double ToValue(double rawValue) => Math.Pow((rawValue - _min) / (_max - _min), 1 / _exp);
        private double ToRawValue(double value) => _min + (_max - _min) * Math.Pow(value, _exp);

        private double _min = 0;
        public double Minimum {
            get => _min;
            set {
                if (_min != value) {
                    _min = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private double _max = 100;
        public double Maximum {
            get => _max;
            set {
                if (_max != value) {
                    _max = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private int _round = 0;
        public int Round {
            get => _round;
            set {
                if (_round != value) {
                    _round = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private double _exp = 100;
        public double Exponent {
            get => _exp;
            set {
                if (_exp != value) {
                    _exp = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private double _value = 0.5;
        public double Value {
            get => _value;
            set {
                value = Math.Max(0, Math.Min(1, value));
                if (value != _value) {
                    _value = value;
                    RawValue = ToRawValue(_value);
                    DrawArc(this.Get<Path>("Arc"), _value);
                }
            }
        }

        private double _raw = 50;
        public double RawValue {
            get => _raw;
            set {
                value = Math.Round(Math.Max(_min, Math.Min(_max, value)) * Math.Pow(10, _round), 0) / Math.Pow(10, _round);
                if (_raw != value) {
                    _raw = value;
                    Value = ToValue(_raw);
                    this.Get<TextBlock>("Display").Text = ValueString;
                    Changed?.Invoke(_raw);
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

        private string _unit = "%";
        public string Unit {
            get => _unit;
            set {
                _unit = value;
                this.Get<TextBlock>("Display").Text = ValueString;
            }
        }

        private string ValueString => $"{RawValue}{Unit}";

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

            DrawArc(this.Get<Path>("ArcBase"), 1);
        }

        private bool mouseHeld = false;
        private double lastY;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;
                Canvas ArcCanvas = this.Get<Canvas>("ArcCanvas");

                lastY = e.GetPosition(ArcCanvas).Y;
                ArcCanvas.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = false;
                this.Get<Canvas>("ArcCanvas").Cursor = new Cursor(StandardCursorType.Arrow);
            }
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
