using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Structures;

namespace Apollo.Components {
    public class Dial: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void DialStartedEventHandler();
        public event DialStartedEventHandler Started;

        public delegate void DialValueChangedEventHandler(double NewValue, double? OldValue);
        public event DialValueChangedEventHandler ValueChanged;

        public delegate void DialStepChangedEventHandler(int NewValue, int? OldValue);
        public event DialStepChangedEventHandler StepChanged;

        public delegate void DialModeChangedEventHandler(bool NewValue, bool? OldValue);
        public event DialModeChangedEventHandler ModeChanged;

        Canvas ArcCanvas;
        Path ArcBase, Arc;
        TextBlock TitleText, Display;
        TextBox Input;

        private const double width = 43, height = 39;
        private const double radius = 18, stroke = 7;
        private const double strokeHalf = stroke / 2;

        private const double angle_start = 4 * Math.PI / 3;
        private const double angle_end = -1 * Math.PI / 3;
        private const double angle_center = Math.PI / 2;

        private double ToValue(double rawValue) => Math.Pow((rawValue - _min) / (_max - _min), 1 / _exp);
        private double ToRawValue(double value) => _min + (_max - _min) * Math.Pow(value, _exp);

        private double _min = 0;
        public double Minimum {
            get => _min;
            set {
                if (_min != value) {
                    _min = value;
                    Value = ToValue(_raw);
                }
            }
        }

        private double _max = 100;
        public double Maximum {
            get => _max;
            set {
                if (_max != value) {
                    _max = value;
                    Value = ToValue(_raw);
                }
            }
        }

        private int _round = 0;
        public int Round {
            get => _round;
            set {
                if (_round != value) {
                    _round = value;
                    Value = ToValue(_raw);
                }
            }
        }

        private double _exp = 1;
        public double Exponent {
            get => _exp;
            set {
                if (_exp != value) {
                    _exp = value;
                    Value = ToValue(_raw);
                }
            }
        }

        private bool _valuechanging = false;
        private double _value = 0.5;
        public double Value {
            get => _value;
            set {
                value = Math.Max(0, Math.Min(1, value));
                if (!_valuechanging && value != _value) {
                    _valuechanging = true;

                    _value = value;
                    RawValue = ToRawValue(_value);
                    DrawArcValue();

                    _valuechanging = false;
                }
            }
        }

        private bool _rawchanging = false;
        private double _raw = 50;
        public double RawValue {
            get => _raw;
            set {
                value = Math.Round(Math.Max(_min, Math.Min(_max, value)) * Math.Pow(10, _round), 0) / Math.Pow(10, _round);
                if (!_rawchanging && _raw != value) {
                    _rawchanging = true;

                    _raw = value;
                    Value = ToValue(_raw);
                    Display.Text = ValueString;

                    ValueChanged?.Invoke(_raw, null);

                    _rawchanging = false;
                }
            }
        }

        private string _title = "Dial";
        public string Title {
            get => _title;
            set {
                TitleText.Text = _title = value;
            }
        }

        private string _unit = "";
        public string Unit {
            get => _unit;
            set {
                _unit = value;
                DrawArcAuto();
            }
        }

        private string _disabledtext = "Disabled";
        public string DisabledText {
            get => _disabledtext;
            set {
                _disabledtext = value;
                
                if (!Enabled) Display.Text = value;
            }
        }
        
        private bool _displaydisabledtext = true;
        public bool DisplayDisabledText {
            get => _displaydisabledtext;
            set {
                _displaydisabledtext = value;

                this.Focus();
                DrawArcAuto();
            }
        }

        private bool _centered = false;
        public bool Centered {
            get => _centered;
            set {
                _centered = value;
                DrawArcAuto();
            }
        }

        private bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                _enabled = value;

                this.Focus();
                DrawArcAuto();
            }
        }

        private double _scale = 1;
        public double Scale {
            get => _scale;
            set {
                value = Math.Max(0, Math.Min(1, value));
                if (value != _scale) {
                    _scale = value;

                    ArcCanvas.Width = width * _scale;
                    ArcCanvas.Height = height * _scale;

                    DrawArcBase();
                    DrawArcAuto();
                }
            }
        }

        private bool _allowSteps = false;
        public bool AllowSteps {
            get => _allowSteps;
            set {
                _allowSteps = value;
                if (!_allowSteps) UsingSteps = false;
            }
        }

        private bool _usingSteps = false;
        public bool UsingSteps {
            get => _usingSteps;
            set {
                if (AllowSteps && Enabled && _usingSteps != value) {
                    _usingSteps = value;
                    DrawArcAuto();
                    
                    ModeChanged?.Invoke(UsingSteps, null);
                }
            }
        }

        private Length _length = new Length();
        public Length Length {
            get => _length;
            set {
                _length = value;
                DrawArcSteps();
            }
        }

        private string ValueString => UsingSteps? _length.ToString() : $"{((_centered && RawValue > 0)? "+" : "")}{RawValue}{Unit}";

        private void DrawArc(Path Arc, double value, bool overrideBase, string color = "ThemeAccentBrush") {
            double angle_starting = (_centered && !overrideBase)? angle_center: angle_start;

            double x_start = (radius * (Math.Cos(angle_starting) + 1) + strokeHalf) * _scale;
            double y_start = (radius * (-Math.Sin(angle_starting) + 1) + strokeHalf) * _scale;
            
            double angle_point = angle_start - Math.Abs(angle_end - angle_start) * (_centered && !overrideBase? value : (1 - (1 - value) * 0.98));

            double x_end = (radius * (Math.Cos(angle_point) + 1) + strokeHalf) * _scale;
            double y_end = (radius * (-Math.Sin(angle_point) + 1) + strokeHalf) * _scale;

            double angle = (angle_starting - angle_point) / Math.PI * 180;

            int large = Convert.ToInt32(angle > 180);
            int direction = Convert.ToInt32(angle > 0);

            Arc.StrokeThickness = stroke * _scale;
            if (!overrideBase) {
                Arc.Stroke = (IBrush)Application.Current.Styles.FindResource(Enabled? color : "ThemeForegroundLowBrush");
                Display.Text = (Enabled || !DisplayDisabledText)? ValueString : DisabledText;
            }
            
            Arc.Data = Geometry.Parse(String.Format("M {0},{1} A {2},{2} {3} {4} {5} {6},{7}",
                x_start.ToString(CultureInfo.InvariantCulture),
                y_start.ToString(CultureInfo.InvariantCulture),
                (radius * _scale).ToString(CultureInfo.InvariantCulture),
                angle.ToString(CultureInfo.InvariantCulture),
                large,
                direction,
                x_end.ToString(CultureInfo.InvariantCulture),
                y_end.ToString(CultureInfo.InvariantCulture)
            ));
        }

        private void DrawArcBase() => DrawArc(ArcBase, 1, true);

        private void DrawArcValue() {
            if (!UsingSteps) DrawArc(Arc, _value, false);
        }

        private void DrawArcSteps() {
            if (UsingSteps) DrawArc(Arc, (double)_length.Step / 9, false, "ThemeExtraBrush");
        }

        public void DrawArcAuto() {
            if (UsingSteps) DrawArcSteps();
            else DrawArcValue();
        }

        public Dial() {
            InitializeComponent();

            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            Input = this.Get<TextBox>("Input");
            Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);

            DrawArcBase();
        }

        private void LayoutChanged(object sender, EventArgs e) => DrawArcAuto();

        private bool mouseHeld = false;
        private double oldValue;
        private int oldStep;
        private double lastY;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left) && Enabled) {
                if (e.ClickCount == 2) {
                    DisplayPressed(sender, e);
                    return;
                }

                mouseHeld = true;
                e.Device.Capture(ArcCanvas);

                lastY = e.GetPosition(ArcCanvas).Y;
                if (UsingSteps) oldStep = Length.Step;
                else oldValue = RawValue;

                Started?.Invoke();

                ArcCanvas.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (!Enabled) return;
            
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = false;
                e.Device.Capture(null);

                if (UsingSteps) StepChanged?.Invoke(Length.Step, oldStep);
                else ValueChanged?.Invoke(RawValue, oldValue);

                ArcCanvas.Cursor = new Cursor(StandardCursorType.Hand);

            } else if (!mouseHeld && e.MouseButton.HasFlag(MouseButton.Right)) {
                Started?.Invoke();
                
                UsingSteps = !UsingSteps;
                
                ModeChanged?.Invoke(UsingSteps, !UsingSteps);
            }
        }

        private void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld && Enabled) {
                double Y = e.GetPosition(ArcCanvas).Y;

                if (UsingSteps) {
                    if (Math.Abs(Y - lastY) >= 8) {
                        _length.Step -= (int)((Y - lastY) / 8);

                        StepChanged?.Invoke(_length.Step, null);

                        DrawArcSteps();
                        lastY = Y;
                    }
                } else {
                    Value += (lastY - Y) / 200;
                    lastY = Y;
                }
            }
        }

        private Action Input_Update;

        private void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            Input_Update = () => { Input.Text = RawValue.ToString(CultureInfo.InvariantCulture); };

            if (int.TryParse(text, out int value)) {
                if (Minimum <= value && value <= Maximum) {
                    RawValue = value;
                    Input_Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    Input_Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                Input_Update += () => {
                    if (value < 0) text = $"-{text.Substring(1).TrimStart('0')}";
                    else if (value > 0) text = text.TrimStart('0');
                    else text = "0";

                    if (Minimum >= 0) {
                        if (value < 0) text = "0";

                    } else {
                        int lower = - (int)Math.Pow(10, ((int)Minimum).ToString(CultureInfo.InvariantCulture).Length - 1) + 1;
                        if (value < lower) text = lower.ToString(CultureInfo.InvariantCulture);
                    }

                    int upper = (int)Math.Pow(10, ((int)Maximum).ToString(CultureInfo.InvariantCulture).Length) - 1;
                    if (value > upper) text = upper.ToString(CultureInfo.InvariantCulture);
                    
                    Input.Text = text;
                };
            }

            if (Minimum < 0 && text == "-") Input_Update = null;

            Dispatcher.UIThread.InvokeAsync(() => {
                Input_Update?.Invoke();
                Input_Update = null;
            });
        }

        private void DisplayPressed(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left && e.ClickCount == 2 && !UsingSteps && Enabled) {
                Input.Text = RawValue.ToString(CultureInfo.InvariantCulture);
                oldValue = RawValue;

                Input.Opacity = 1;
                Input.IsHitTestVisible = true;
                Input.Focus();

                Started?.Invoke();

                e.Handled = true;
            }
        }
        
        private void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = RawValue.ToString(CultureInfo.InvariantCulture);

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;

            ValueChanged?.Invoke(_raw, oldValue);
        }

        private void Input_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        private void Input_KeyUp(object sender, KeyEventArgs e) => e.Key = Key.None;

        private void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
