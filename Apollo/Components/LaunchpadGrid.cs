using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Structures;

namespace Apollo.Components {
    public class LaunchpadGrid: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        StackPanel Root;
        UniformGrid Grid;
        Path TopLeft, TopRight, BottomLeft, BottomRight;
        Shape ModeLight;

        public delegate void PadChangedEventHandler(int index);
        public event PadChangedEventHandler PadStarted;
        public event PadChangedEventHandler PadFinished;
        public event PadChangedEventHandler PadPressed;
        public event PadChangedEventHandler PadReleased;

        public delegate void PadModsChangedEventHandler(int index, InputModifiers mods);
        public event PadModsChangedEventHandler PadModsPressed;

        public static int GridToSignal(int index) => (index == -1)? 99 : ((9 - (index / 10)) * 10 + index % 10);
        public static int SignalToGrid(int index) => (index == 99)? -1 : ((9 - (index / 10)) * 10 + index % 10);

        public void SetColor(int index, SolidColorBrush color) {
            if (index == 0 || index == 9 || index == 90 || index == 99) return;

            if (index == -1) ModeLight.Fill = color;
            else ((Shape)Grid.Children[index]).Fill = color;
        }

        private double _scale = 1;
        public double Scale {
            get => _scale;
            set {
                value = Math.Max(0, value);
                if (value != _scale) {
                    _scale = value;

                    this.Resources["PadSquareSize"] = 15 * Scale;
                    this.Resources["PadCircleSize"] = 11 * Scale;
                    this.Resources["PadCut1"] = 3 * Scale;
                    this.Resources["PadCut2"] = 12 * Scale;
                    this.Resources["ModeWidth"] = 4 * Scale;
                    this.Resources["ModeHeight"] = 2 * Scale;
                    this.Resources["PadMargin"] = new Thickness(1 * Scale);
                    this.Resources["ModeMargin"] = new Thickness(0, 5 * Scale, 0, 0);
                    
                    DrawPath();
                }
            }
        }
        
        private bool _lowQuality = false;
        public bool LowQuality {
            get => _lowQuality;
            set {
                if (value != _lowQuality) {
                    _lowQuality = value;

                    ModeLight.Opacity = Convert.ToInt32(!LowQuality);
                    
                    DrawPath();
                }
            }
        }

        private const string LowQualityPadData = "M 0,0 L 0,{0} {0},{0} {0},0 Z";

        public string FormatPath(string format) => String.Format(format,
            ((double)this.Resources["PadSquareSize"] + (LowQuality? 0.5 : 0)).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadCut1"]).ToString(CultureInfo.InvariantCulture),
            ((double)this.Resources["PadCut2"]).ToString(CultureInfo.InvariantCulture)
        );

        public void DrawPath() {
            TopLeft.Data = Geometry.Parse(FormatPath(LowQuality? LowQualityPadData : "M 0,0 L 0,{0} {2},{0} {0},{2} {0},0 Z"));
            TopRight.Data = Geometry.Parse(FormatPath(LowQuality? LowQualityPadData : "M 0,0 L 0,{2} {1},{0} {0},{0} {0},0 Z"));
            BottomLeft.Data = Geometry.Parse(FormatPath(LowQuality? LowQualityPadData : "M 0,0 L 0,{0} {0},{0} {0},{1} {2},0 Z"));
            BottomRight.Data = Geometry.Parse(FormatPath(LowQuality? LowQualityPadData : "M 0,{1} L 0,{0} {0},{0} {0},0 {1},0 Z"));
        }

        public LaunchpadGrid() {
            InitializeComponent();

            Root = this.Get<StackPanel>("Root");
            Grid = this.Get<UniformGrid>("LaunchpadGrid");

            TopLeft = this.Get<Path>("TopLeft");
            TopRight = this.Get<Path>("TopRight");
            BottomLeft = this.Get<Path>("BottomLeft");
            BottomRight = this.Get<Path>("BottomRight");

            ModeLight = this.Get<Rectangle>("ModeLight");
        }

        private void LayoutChanged(object sender, EventArgs e) => DrawPath();

        public void RenderFrame(Frame frame) {
            for (int i = 0; i < 100; i++)
                SetColor(SignalToGrid(i), frame.Screen[i].ToScreenBrush());
        }

        bool mouseHeld = false;
        Shape mouseOver = null;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;

                e.Device.Capture(Root);
                Root.Cursor = new Cursor(StandardCursorType.Hand);

                PadStarted?.Invoke(Grid.Children.IndexOf((IControl)sender));
                MouseMove(sender, e);
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                MouseMove(sender, e);
                PadFinished?.Invoke(Grid.Children.IndexOf((IControl)sender));

                mouseHeld = false;
                if (mouseOver != null) MouseLeave(mouseOver);
                mouseOver = null;

                e.Device.Capture(null);
                Root.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        private void MouseEnter(Shape control, InputModifiers mods) {
            int index = Grid.Children.IndexOf((IControl)control);
            PadPressed?.Invoke(index);
            PadModsPressed?.Invoke(index, mods);
        }

        private void MouseLeave(Shape control) => PadReleased?.Invoke(Grid.Children.IndexOf((IControl)control));

        private void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                IInputElement _over = Root.InputHitTest(e.Device.GetPosition(Root));

                if (_over is Shape) {
                    Shape over = (Shape)_over;
                    
                    if (mouseOver == null) MouseEnter(over, e.InputModifiers);
                    else if (mouseOver != over) {
                        MouseLeave(mouseOver);
                        MouseEnter(over, e.InputModifiers);
                    }

                    mouseOver = over;

                } else if (mouseOver != null) {
                    MouseLeave(mouseOver);
                    mouseOver = null;
                }
            }
        }
    }
}
