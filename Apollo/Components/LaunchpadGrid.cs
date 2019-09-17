using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Core;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Components {
    public class LaunchpadGrid: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<LayoutTransformControl>("Root");
            View = this.Get<Viewbox>("View");
            Back = this.Get<Border>("Back");

            ModeLight = this.Get<Rectangle>("ModeLight");
        }
        
        LayoutTransformControl Root;
        Viewbox View;
        Grid Grid;
        Border Back;
        Path[] Elements;
        Rectangle ModeLight;

        public delegate void PadChangedEventHandler(int index);
        public event PadChangedEventHandler PadStarted;
        public event PadChangedEventHandler PadFinished;
        public event PadChangedEventHandler PadPressed;
        public event PadChangedEventHandler PadReleased;

        public delegate void PadModsChangedEventHandler(int index, KeyModifiers mods);
        public event PadModsChangedEventHandler PadModsPressed;

        public static int GridToSignal(int index) => (index == -1)? 100 : ((9 - (index / 10)) * 10 + index % 10);
        public static int SignalToGrid(int index) => (index == 100)? -1 : ((9 - (index / 10)) * 10 + index % 10);

        bool IsPhantom(int index) {
            if (Preferences.LaunchpadStyle == LaunchpadStyles.Stock) {
                int x = index % 10;
                int y = index / 10;

                if (x == 0 || x == 9 || y == 0 || y == 9) return true;
            }

            return Preferences.LaunchpadStyle == LaunchpadStyles.Phantom;
        }

        public void SetColor(int index, SolidColorBrush color) {
            if (index == 0 || index == 9 || index == 90 || index == 99) return;

            if (index == -1) {
                if (IsArrangeValid) ModeLight.Fill = color;
                else this.Resources["ModeBrush"] = color;
            }

            else if (LowQuality) Elements[index].Fill = color;
            else Elements[index].Stroke = IsPhantom(index)? color : Elements[index].Fill = color;
        }

        public void Clear() {
            SolidColorBrush color = (SolidColorBrush)Application.Current.Styles.FindResource("ThemeForegroundLowBrush");
            for (int i = -1; i < 100; i++) SetColor(i, color);
        }

        void Update_LaunchpadStyle() {
            if (LowQuality) return;

            for (int i = 0; i < 100; i++) {
                if (i == 0 || i == 9 || i == 90 || i == 99) continue;

                Elements[i].Fill = IsPhantom(i)? SolidColorBrush.Parse("Transparent") : Elements[i].Stroke;
            }
        }

        double _scale = 1;
        public double Scale {
            get => _scale;
            set {
                if ((value = Math.Max(0, value)) != _scale) {
                    _scale = value;

                    ApplyScale();
                }
            }
        }

        double EffectiveScale => Scale * ((Preferences.LaunchpadGridRotation && !LowQuality)? 0.702 : 1);
        
        bool _lowQuality = false;
        public bool LowQuality {
            get => _lowQuality;
            set {
                if (value != _lowQuality) {
                    _lowQuality = value;

                    ApplyScale();
                }
            }
        }

        readonly Geometry LowQualityGeometry = Geometry.Parse("M 0,0 L 0,1 1,1 1,0 Z");

        public Geometry SquareGeometry => Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
            ((double)this.Resources["PadSize"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadThickness"] / 2).ToString()
        ));

        public Geometry CircleGeometry => Geometry.Parse(String.Format("M {0},{1} A {2},{2} 180 1 1 {0},{3} A {2},{2} 180 1 1 {0},{1} Z",
            ((double)this.Resources["PadSize"] / 2).ToString(),
            ((double)this.Resources["PadSize"] / 8 + (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadSize"] * 3 / 8 - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadSize"] * 7 / 8 - (double)this.Resources["PadThickness"] / 2).ToString()
        ));

        public Geometry CreateCornerGeometry(string format) => Geometry.Parse(String.Format(format,
            ((double)this.Resources["PadSize"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadCut1"] + (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadCut2"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadThickness"] / 2).ToString()
        ));

        public void DrawPath() {
            this.Resources["SquareGeometry"] = LowQuality? LowQualityGeometry : SquareGeometry;
            this.Resources["CircleGeometry"] = LowQuality? LowQualityGeometry : CircleGeometry;

            Elements[44].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{0} {2},{0} {0},{2} {0},{3} Z");
            Elements[45].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{2} {1},{0} {0},{0} {0},{3} Z");
            Elements[54].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{0} {0},{0} {0},{1} {2},{3} Z");
            Elements[55].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{1} L {3},{0} {0},{0} {0},{3} {1},{3} Z");
        }

        void ApplyScale() {
            this.Resources["Rotation"] = (Preferences.LaunchpadGridRotation && !LowQuality)? -45.0 : 0.0;
            this.Resources["CanvasSize"] = 184 * Scale;
            this.Resources["PadSize"] = 15 * EffectiveScale;
            this.Resources["PadThickness"] = LowQuality? 0 : 1 * EffectiveScale;
            this.Resources["PadCut1"] = 3 * EffectiveScale;
            this.Resources["PadCut2"] = 12 * EffectiveScale;
            this.Resources["ModeWidth"] = 4 * EffectiveScale;
            this.Resources["ModeHeight"] = 2 * EffectiveScale;
            this.Resources["TopMargin"] = new Thickness((int)(7 * EffectiveScale), (int)(7 * EffectiveScale), (int)(7 * EffectiveScale), 0);
            this.Resources["PadMargin"] = new Thickness((int)(1 * EffectiveScale));
            this.Resources["ModeMargin"] = new Thickness(0, (int)(5 * EffectiveScale), 0, 0);
            this.Resources["CornerRadius"] = new CornerRadius((int)(1 * EffectiveScale));

            string gridSize = (17 * EffectiveScale).ToString();
            string GridDefinitions = String.Join(",", (from i in Enumerable.Range(0, 10) select gridSize).ToArray());

            for (int i = 99; i >= 0; i--) Grid.Children.RemoveAt(i);

            View.Child = Grid = new Grid() {
                RowDefinitions = RowDefinitions.Parse(GridDefinitions),
                ColumnDefinitions = ColumnDefinitions.Parse(GridDefinitions)
            };

            for (int i = 0; i < 100; i++) Grid.Children.Add(Elements[i]);

            ModeLight.Opacity = Back.Opacity = Convert.ToInt32(!LowQuality);
            
            DrawPath();
        }

        public LaunchpadGrid() {
            InitializeComponent();

            View.Child = Grid = new Grid() {
                RowDefinitions = RowDefinitions.Parse("*,*,*,*,*,*,*,*,*,*"),
                ColumnDefinitions = ColumnDefinitions.Parse("*,*,*,*,*,*,*,*,*,*")
            };

            Elements = new Path[100];
            for (int i = 0; i < 100; i++) {
                Grid.Children.Add(Elements[i] = new Path());

                int x = i % 10;
                int y = i / 10;

                Grid.SetRow(Elements[i], y);
                Grid.SetColumn(Elements[i], x);

                if (i == 0 || i == 9 || i == 90 || i == 99) Elements[i].Classes.Add("empty");
                else {
                    Elements[i].PointerPressed += MouseDown;

                    if (x == 0 || x == 9 || y == 0 || y == 9) Elements[i].Classes.Add("circle");
                    else if (i == 44 || i == 45 || i == 54 || i == 55) Elements[i].Classes.Add("corner");
                    else Elements[i].Classes.Add("square");
                }
            }

            Preferences.LaunchpadStyleChanged += Update_LaunchpadStyle;
            Preferences.LaunchpadGridRotationChanged += ApplyScale;

            Clear();
            ApplyScale();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            PadStarted = null;
            PadFinished = null;
            PadPressed = null;
            PadReleased = null;
            PadModsPressed = null;

            for (int i = 0; i < 100; i++)
                if (!(i == 0 || i == 9 || i == 90 || i == 99)) 
                    Elements[i].PointerPressed -= MouseDown;

            Preferences.LaunchpadStyleChanged -= Update_LaunchpadStyle;
            Preferences.LaunchpadGridRotationChanged -= ApplyScale;

            Clear();
        }

        void LayoutChanged(object sender, EventArgs e) => DrawPath();

        public void RenderFrame(Frame frame) {
            for (int i = 0; i < 101; i++)
                SetColor(SignalToGrid(i), frame.Screen[i].ToScreenBrush());
        }

        bool mouseHeld = false;
        Shape mouseOver = null;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;

                e.Pointer.Capture(Root);
                Root.Cursor = new Cursor(StandardCursorType.Hand);

                PadStarted?.Invoke(Array.IndexOf(Elements, (IControl)sender));
                MouseMove(sender, e);
            }
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                MouseMove(sender, e);
                PadFinished?.Invoke(Array.IndexOf(Elements, (IControl)sender));

                mouseHeld = false;
                if (mouseOver != null) MouseLeave(mouseOver);
                mouseOver = null;

                e.Pointer.Capture(null);
                Root.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        void MouseEnter(Shape control, KeyModifiers mods) {
            int index = Array.IndexOf(Elements, (IControl)control);
            PadPressed?.Invoke(index);
            PadModsPressed?.Invoke(index, mods);
        }

        void MouseLeave(Shape control) => PadReleased?.Invoke(Array.IndexOf(Elements, (IControl)control));

        void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                IInputElement _over = Root.InputHitTest(e.GetPosition(Root));

                if (_over is Shape over) {
                    if (mouseOver == null) MouseEnter(over, e.KeyModifiers);
                    else if (mouseOver != over) {
                        MouseLeave(mouseOver);
                        MouseEnter(over, e.KeyModifiers);
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
