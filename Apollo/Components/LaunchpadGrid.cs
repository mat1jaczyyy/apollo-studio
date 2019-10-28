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
        Canvas[] Canvases;
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
            if (Preferences.LaunchpadModel == LaunchpadModels.X && index == 9) return false;

            if (Preferences.LaunchpadStyle == LaunchpadStyles.Stock) {
                int x = index % 10;
                int y = index / 10;

                if (x == 0 || x == 9 || y == 0 || y == 9) return true;
            }

            return Preferences.LaunchpadStyle == LaunchpadStyles.Phantom;
        }

        public void SetColor(int index, SolidColorBrush color) {
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

        void AddClass(int index, string className) {
            Canvases[index].Classes.Add(className);
            Elements[index].Classes.Add(className);
        }

        void Update_LaunchpadStyle() {
            if (LowQuality) return;

            for (int i = 0; i < 100; i++) {
                Elements[i].Fill = IsPhantom(i)? SolidColorBrush.Parse("Transparent") : Elements[i].Stroke;
            }
        }

        void Update_LaunchpadModel() {
            ApplyScale();

            for (int i = 0; i < 100; i++) {
                int x = i % 10;
                int y = i / 10;

                if (!Canvases[i].Classes.Contains("empty"))
                    Canvases[i].PointerPressed -= MouseDown;

                Canvases[i].Classes.Clear();
                Elements[i].Classes.Clear();

                switch (Preferences.LaunchpadModel) {
                    case LaunchpadModels.MK2:
                        if (x == 0 || y == 9 || i == 9) AddClass(i, "empty");
                        else {
                            Canvases[i].PointerPressed += MouseDown;

                            if (x == 9 || y == 0) AddClass(i, "circle");
                            else if (i == 44 || i == 45 || i == 54 || i == 55) AddClass(i, "corner");
                            else AddClass(i, "square");
                        }
                        break;

                    case LaunchpadModels.Pro:
                        if (i == 0 || i == 9 || i == 90 || i == 99) AddClass(i, "empty");
                        else {
                            Canvases[i].PointerPressed += MouseDown;

                            if (x == 0 || x == 9 || y == 0 || y == 9) AddClass(i, "circle");
                            else if (i == 44 || i == 45 || i == 54 || i == 55) AddClass(i, "corner");
                            else AddClass(i, "square");
                        }
                        break;

                    case LaunchpadModels.X:
                        if (x == 0 || y == 9) AddClass(i, "empty");
                        else {
                            Canvases[i].PointerPressed += MouseDown;

                            if (i == 9) AddClass(i, "novation");
                            else if (i == 44 || i == 45 || i == 54 || i == 55) AddClass(i, "corner");
                            else AddClass(i, "square");
                        }
                        break;

                    case LaunchpadModels.All:
                        Canvases[i].PointerPressed += MouseDown;

                        if (i == 0 || i == 9 || i == 90 || i == 99) AddClass(i, "hidden");
                        else if (i == 44 || i == 45 || i == 54 || i == 55) AddClass(i, "corner");
                        else AddClass(i, "square");
                        break;
                }
            }

            Update_LaunchpadStyle();
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

        public Geometry NovationGeometry => Geometry.Parse(String.Format("F1 M {0},0 L {1},0 C {2},0 0,{2} 0,{1} L 0,{0} C 0,{3} {2},{4} {1},{4} L {0},{4} C {3},{4} {4},{3} {4},{0} L {4},{1} C {4},{2} {3},0 {0},0 Z M {5},{6} L {7},{8} {9},{10} {11},{12} Z M {13},{14} C {15},{16} {17},{18} {19},{20} L {21},{22} {23},{24} {25},{11} {26},{27} C {28},{29} {15},{30} {13},{31} {13},{6} {32},{33} {13},{14} Z",
            (((double)this.Resources["NovationSize"]) / 11 * 10.08).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 0.8).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 0.32).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 10.56).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 10.88).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 0.96).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 5.28).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 5.6).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 0.48).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 7.84).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 2.72).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 3.2).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 7.52).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 9.76).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 6.08).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 9.6).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 6.56).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 9.44).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 6.88).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 9.12).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 7.2).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 5.92).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 10.24).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 3.68).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 8).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 8.32).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 8.96).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 3.84).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 9.28).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 4.16).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 4.48).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 4.96).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 9.92).ToString(),
            (((double)this.Resources["NovationSize"]) / 11 * 5.76).ToString()
        ));

        public Geometry HiddenGeometry => Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
            ((double)this.Resources["HiddenSize"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadThickness"] / 2).ToString()
        ));

        public void DrawPath() {
            this.Resources["SquareGeometry"] = LowQuality? LowQualityGeometry : SquareGeometry;
            this.Resources["CircleGeometry"] = LowQuality? LowQualityGeometry : CircleGeometry;
            this.Resources["NovationGeometry"] = LowQuality? LowQualityGeometry : NovationGeometry;
            this.Resources["HiddenGeometry"] = LowQuality? LowQualityGeometry : HiddenGeometry;

            Elements[44].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{0} {2},{0} {0},{2} {0},{3} Z");
            Elements[45].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{2} {1},{0} {0},{0} {0},{3} Z");
            Elements[54].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{3} L {3},{0} {0},{0} {0},{1} {2},{3} Z");
            Elements[55].Data = LowQuality? LowQualityGeometry : CreateCornerGeometry("M {3},{1} L {3},{0} {0},{0} {0},{3} {1},{3} Z");
        }

        void ApplyScale() {
            bool is10x10 = Preferences.LaunchpadModel == LaunchpadModels.Pro || Preferences.LaunchpadModel == LaunchpadModels.All;

            this.Resources["Rotation"] = (Preferences.LaunchpadGridRotation && !LowQuality)? -45.0 : 0.0;
            this.Resources["CanvasSize"] = (is10x10? 184 : 167) * Scale;
            this.Resources["PadSize"] = 15 * EffectiveScale;
            this.Resources["NovationSize"] = 11 * EffectiveScale;
            this.Resources["HiddenSize"] = 7 * EffectiveScale;
            this.Resources["PadThickness"] = LowQuality? 0 : 1 * EffectiveScale;
            this.Resources["PadCut1"] = 3 * EffectiveScale;
            this.Resources["PadCut2"] = 12 * EffectiveScale;
            this.Resources["ModeWidth"] = 4 * EffectiveScale;
            this.Resources["ModeHeight"] = 2 * EffectiveScale;
            this.Resources["TopMargin"] = new Thickness((int)(7 * EffectiveScale), (int)(7 * EffectiveScale), (int)(7 * EffectiveScale), 0);
            this.Resources["PadMargin"] = new Thickness((int)(1 * EffectiveScale));
            this.Resources["ModeMargin"] = new Thickness(0, (int)(5 * EffectiveScale), 0, 0);
            this.Resources["CornerRadius"] = new CornerRadius((int)(1 * EffectiveScale));

            int buttons = is10x10? 10 : 9;
            string gridSize = (17 * EffectiveScale).ToString();
            
            for (int i = 99; i >= 0; i--) Grid.Children.RemoveAt(i);

            View.Child = Grid = new Grid() {
                RowDefinitions = RowDefinitions.Parse(
                    String.Join(
                        ",",
                        (from i in Enumerable.Range(0, buttons) select gridSize).Concat(from i in Enumerable.Range(0, 10 - buttons) select "0").ToArray()
                    )
                ),
                ColumnDefinitions = ColumnDefinitions.Parse(
                    String.Join(
                        ",",
                        (from i in Enumerable.Range(0, 10 - buttons) select "0").Concat(from i in Enumerable.Range(0, buttons) select gridSize).ToArray()
                    )
                )
            };

            for (int i = 0; i < 100; i++) Grid.Children.Add(Canvases[i]);

            Back.Opacity = Convert.ToInt32(!LowQuality);
            ModeLight.Opacity = Convert.ToInt32(ModeLight.IsHitTestVisible = (!LowQuality && is10x10));
            
            DrawPath();
        }

        public LaunchpadGrid() {
            InitializeComponent();
            
            View.Child = Grid = new Grid() {
                RowDefinitions = RowDefinitions.Parse("*,*,*,*,*,*,*,*,*,*"),
                ColumnDefinitions = ColumnDefinitions.Parse("*,*,*,*,*,*,*,*,*,*")
            };

            Canvases = new Canvas[100];
            Elements = new Path[100];
            for (int i = 0; i < 100; i++) {
                Grid.Children.Add(Canvases[i] = new Canvas());
                Canvases[i].Children.Add(Elements[i] = new Path());

                Grid.SetRow(Canvases[i], i / 10);
                Grid.SetColumn(Canvases[i], i % 10);

                AddClass(i, "empty");
            }

            Preferences.LaunchpadStyleChanged += Update_LaunchpadStyle;
            Preferences.LaunchpadGridRotationChanged += ApplyScale;
            Preferences.LaunchpadModelChanged += Update_LaunchpadModel;

            Update_LaunchpadModel();

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
                if (!Canvases[i].Classes.Contains("empty"))
                    Canvases[i].PointerPressed -= MouseDown;

            Preferences.LaunchpadStyleChanged -= Update_LaunchpadStyle;
            Preferences.LaunchpadGridRotationChanged -= ApplyScale;
            Preferences.LaunchpadModelChanged -= Update_LaunchpadModel;

            Clear();
        }

        void LayoutChanged(object sender, EventArgs e) => DrawPath();

        public void RenderFrame(Frame frame) {
            for (int i = 0; i < 101; i++)
                SetColor(SignalToGrid(i), frame.Screen[i].ToScreenBrush());
        }

        bool mouseHeld = false;
        Canvas mouseOver = null;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) {
                mouseHeld = true;

                e.Pointer.Capture(Root);
                Root.Cursor = new Cursor(StandardCursorType.Hand);

                PadStarted?.Invoke(Array.IndexOf(Canvases, (IControl)sender));
                MouseMove(sender, e);
            }
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) {
                MouseMove(sender, e);
                PadFinished?.Invoke(Array.IndexOf(Canvases, (IControl)sender));

                mouseHeld = false;
                if (mouseOver != null) MouseLeave(mouseOver);
                mouseOver = null;

                e.Pointer.Capture(null);
                Root.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        void MouseEnter(Canvas control, KeyModifiers mods) {
            int index = Array.IndexOf(Canvases, control);
            PadPressed?.Invoke(index);
            PadModsPressed?.Invoke(index, mods);
        }

        void MouseLeave(Canvas control) => PadReleased?.Invoke(Array.IndexOf(Canvases, control));

        void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                IInputElement _over = Root.InputHitTest(e.GetPosition(Root));

                if (_over is Shape overPath)
                    _over = overPath.Parent;
                
                if (_over is Canvas over) {
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
