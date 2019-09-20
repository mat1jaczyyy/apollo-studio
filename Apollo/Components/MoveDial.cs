using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace Apollo.Components {
    public class MoveDial: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            PlaneCanvas = this.Get<Canvas>("PlaneCanvas");
            XRect = this.Get<Rectangle>("XRect");
            YRect = this.Get<Rectangle>("YRect");
            Point = this.Get<Rectangle>("Point");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            InputX = this.Get<TextBox>("InputX");
            InputY = this.Get<TextBox>("InputY");
        }

        public delegate void MoveDialChangedEventHandler(int x, int y, int? old_x, int? old_y);
        public event MoveDialChangedEventHandler Changed;

        Canvas PlaneCanvas;
        Rectangle XRect, YRect, Point;
        TextBlock TitleText, Display;
        TextBox InputX, InputY;

        int _x = 0;
        public int X {
            get => _x;
            set {
                value = Math.Max(-9, Math.Min(9, value));
                if (value != _x) {
                    _x = value;
                    DrawX();
                    DrawPoint();
                    Changed?.Invoke(_x, _y, null, null);
                }
            }
        }

        int _y = 0;
        public int Y {
            get => _y;
            set {
                value = Math.Max(-9, Math.Min(9, value));
                if (value != _y) {
                    _y = value;
                    DrawY();
                    DrawPoint();
                    Changed?.Invoke(_x, _y, null, null);
                }
            }
        }

        string _title = "Dial";
        public string Title {
            get => _title;
            set {
                TitleText.Text = _title = value;
            }
        }

        string ValueString => $"({_x}, {_y})";

        void DrawPoint() {
            Canvas.SetLeft(Point, 18 + 2 * _x);
            Canvas.SetTop(Point, 18 - 2 * _y);

            Display.Text = ValueString;
        }

        void DrawX() {
            XRect.Width = Math.Abs(2 * _x) + 2;
            Canvas.SetLeft(XRect, (_x > 0)? 18 : 20 - XRect.Width);
        }

        void DrawY() {
            YRect.Height = Math.Abs(2 * _y) + 2;
            Canvas.SetTop(YRect, (_y < 0)? 18 : 20 - YRect.Height);
        }

        public MoveDial() {
            InitializeComponent();

            InputX.GetObservable(TextBox.TextProperty).Subscribe(InputX_Changed);
            InputX.AddHandler(InputElement.PointerPressedEvent, Input_MouseDown, RoutingStrategies.Tunnel);

            InputY.GetObservable(TextBox.TextProperty).Subscribe(InputY_Changed);
            InputY.AddHandler(InputElement.PointerPressedEvent, Input_MouseDown, RoutingStrategies.Tunnel);

            DrawX();
            DrawY();
            DrawPoint();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Changed = null;

            InputX.RemoveHandler(InputElement.PointerPressedEvent, Input_MouseDown);
            InputY.RemoveHandler(InputElement.PointerPressedEvent, Input_MouseDown);
        }

        bool mouseHeld = false;
        int old_x, old_y;
        double lastX, lastY;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetPointerPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) {
                if (e.ClickCount == 2) {
                    DisplayPressed(sender, e);
                    return;
                }

                mouseHeld = true;
                e.Pointer.Capture(PlaneCanvas);

                lastX = e.GetPosition(PlaneCanvas).X;
                lastY = e.GetPosition(PlaneCanvas).Y;
                old_x = X;
                old_y = Y;

                PlaneCanvas.Cursor = new Cursor(StandardCursorType.SizeAll);
            }
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetPointerPoint(this).Properties.PointerUpdateKind;
            
            if (MouseButton == PointerUpdateKind.LeftButtonReleased) {
                mouseHeld = false;
                e.Pointer.Capture(null);

                if (old_x != X || old_y != Y)
                    Changed?.Invoke(X, Y, old_x, old_y);

                PlaneCanvas.Cursor = new Cursor(StandardCursorType.Hand);
            }
        }

        void MouseMove(object sender, PointerEventArgs e) {
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

        Action InputX_Update = null, InputY_Update = null;

        void InputX_Changed(string text) => X = Input_Changed(InputX, InputX_Update, X, text);
        void InputY_Changed(string text) => Y = Input_Changed(InputY, InputY_Update, Y, text);

        int Input_Changed(TextBox Input, Action Update, int RawValue, string text) {
            if (text == null) return RawValue;
            if (text == "") return RawValue;

            Update = () => { Input.Text = RawValue.ToString(); };

            if (int.TryParse(text, out int value)) {
                if (-9 <= value && value <= 9) {
                    RawValue = value;
                    Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                Update += () => {
                    if (value < 0) text = $"-{text.Substring(1).TrimStart('0')}";
                    else if (value > 0) text = text.TrimStart('0');
                    else text = "0";

                    if (value < -9) text = "-9";
                    if (value > 9) text = "9";
                    
                    Input.Text = text;
                };
            }

            if (text == "-") Update = null;

            Dispatcher.UIThread.InvokeAsync(() => {
                Update?.Invoke();
                Update = null;
            });

            return RawValue;
        }

        void DisplayPressed(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetPointerPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed && e.ClickCount == 2) {
                InputX.Text = X.ToString();
                InputY.Text = Y.ToString();

                InputX.SelectionStart = 0;
                InputX.SelectionEnd = InputX.Text.Length;
                InputX.CaretIndex = InputX.Text.Length;

                Display.Opacity = 0;
                Display.IsHitTestVisible = false;

                InputX.Opacity = InputY.Opacity = 1;
                InputX.IsHitTestVisible = InputY.IsHitTestVisible = true;
                InputX.Focus();

                e.Handled = true;
            }
        }

        bool SkipLostFocus = false;
        
        void Input_LostFocus(object sender, RoutedEventArgs e) {
            if (SkipLostFocus) {
                SkipLostFocus = false;
                return;
            }

            InputX.Text = X.ToString();
            InputY.Text = Y.ToString();

            Display.Opacity = 1;
            Display.IsHitTestVisible = true;

            InputX.Opacity = InputY.Opacity = 0;
            InputX.IsHitTestVisible = InputY.IsHitTestVisible = false;

            if (old_x != X || old_y != Y)
                Changed?.Invoke(X, Y, old_x, old_y);
        }

        void Input_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        void Input_KeyUp(object sender, KeyEventArgs e) => e.Key = Key.None;

        void Input_MouseDown(object sender, PointerPressedEventArgs e) {
            TextBox Input = (TextBox)sender;

            if (!Input.IsFocused) {
                SkipLostFocus = true;
                Input.Focus();
            }
        }

        void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
