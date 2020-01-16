using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Structures;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Apollo.Components {
    public class GraphDial: UserControl {
        TextBlock Title, Display;
        TextBox Input;
        Canvas PathCanvas;
        Path Curve;
        
        IDisposable observable;

        
        public delegate void DialStartedEventHandler();
        public event DialStartedEventHandler Started;

        public delegate void DialValueChangedEventHandler(GraphDial sender, double NewValue, double? OldValue);
        public event DialValueChangedEventHandler ValueChanged;

        public delegate void IsPrimaryChangedEventHandler(bool NewValue, bool? OldValue);
        public event IsPrimaryChangedEventHandler ModeChanged;
        
        public double Value {
            get => _value;
            set {
                if(-2 <= value && value <= 2){
                    _value = Math.Round(value, 1);
                    
                    Display.Text = Value.ToString();
                    DrawGraph();
                }
            }
        }
        
        double _value = 0;
        
        public Func<double, double> Function;
        bool IsPrimary = true;
        public bool Enabled = true;
        
        bool mouseHeld = false;
        double oldValue;
        double lastY;
        
        protected void LayoutChanged(object sender, EventArgs e) => DrawGraph();
        
        protected virtual void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Title = this.Get<TextBlock>("Title");
            Display = this.Get<TextBlock>("Display");
            
            Input = this.Get<TextBox>("Input");
            
            PathCanvas = this.Get<Canvas>("PathCanvas");
            
            Curve = this.Get<Path>("Curve");
        }
        
        public GraphDial(){
            InitializeComponent();
            
            IsPrimary = true;
            
            Curve.StrokeThickness = 2;
            Curve.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush");
            
            observable = Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);
            
            Title.Text = "Pinch";
            
            DrawGraph();
        }
        
        protected void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
        }
        
        Action Input_Update;
        
        protected void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            Input_Update = () => { Input.Text = Value.ToString(); };

            if (double.TryParse(text, out double value)) {
                if (-2 <= value && value <= 2) {
                    Value = value;
                    Input_Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    Input_Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                Input_Update += () => {
                    if (value <= -1) text = $"-{text.Substring(1).TrimStart('0')}";
                    else if (value >= 1) text = text.TrimStart('0');
                    else if (!text.Contains('.') && text[0] != '-') text = "0";
                    
                    Input.Text = text;
                };
            }

            Dispatcher.UIThread.InvokeAsync(() => {
                Input_Update?.Invoke();
                Input_Update = null;
            });
        }
        
        protected void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed && Enabled) {
                if (e.KeyModifiers.HasFlag(App.ControlKey)) {
                    Value = 0;
                    return;
                }
                
                if(e.ClickCount == 2){
                    DisplayPressed(sender, e);
                    return;
                }

                mouseHeld = true;
                e.Pointer.Capture(PathCanvas);

                lastY = e.GetPosition(PathCanvas).Y;
                oldValue = Value;

                Started?.Invoke();

                PathCanvas.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            }
        }
        
        protected void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (!Enabled) return;
            
            if (MouseButton == PointerUpdateKind.LeftButtonReleased) {
                mouseHeld = false;
                e.Pointer.Capture(null);

                ValueChanged?.Invoke(this, Value, oldValue);

                PathCanvas.Cursor = new Cursor(StandardCursorType.Hand);

            } else if (!mouseHeld && MouseButton == PointerUpdateKind.RightButtonReleased) {
                Started?.Invoke();
                
                IsPrimary = !IsPrimary;
                
                DrawGraph();
                
                ModeChanged?.Invoke(IsPrimary, !IsPrimary);
            }
        }
        
        protected void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld && Enabled) {
                double Y = e.GetPosition(PathCanvas).Y;
                
                Value += (lastY - Y) / 100;
                lastY = Y;
            }
        }
        
        void DrawGraph(){
            if(IsPrimary) Curve.Data = Geometry.Parse(String.Format("M 0 50 Q {0} {0} 50 0", 
                    (25 + (int)Math.Round(-Value * 12.5)).ToString()
                ));
            else Curve.Data = Geometry.Parse(String.Format("M 0 50 C {0} {1} {2} {3} 50 0",
                    (25 + (int)Math.Round(-Value * 12.5)).ToString(),
                    (25 + (int)Math.Round(-Value * 12.5)).ToString(),
                    (25 + (int)Math.Round(Value * 12.5)).ToString(),
                    (25 + (int)Math.Round(Value * 12.5)).ToString()
                ));            
        }
        
        protected void DisplayPressed(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed && e.ClickCount == 2 && Enabled) {
                Input.Text = Value.ToString();
                oldValue = Value;

                Input.SelectionStart = 0;
                Input.SelectionEnd = Input.Text.Length;
                Input.CaretIndex = Input.Text.Length;

                Input.Opacity = 1;
                Input.IsHitTestVisible = true;
                Input.Focus();

                Started?.Invoke();

                e.Handled = true;
            }
        }
        
        protected void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = Value.ToString();

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;

            ValueChanged?.Invoke(this, Value, oldValue);
        }

        protected void Input_KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        protected void Input_KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        protected void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}