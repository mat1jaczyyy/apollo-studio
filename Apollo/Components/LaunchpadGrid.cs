using System;
using System.Collections.Generic;
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
            View = this.Get<Border>("View");
            Back = this.Get<Border>("Back");

            ModeLight = this.Get<Rectangle>("ModeLight");
        }
        
        LayoutTransformControl Root;
        Border View, Back;
        Grid Grid;
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
            if (index == -1)
                this.Resources["ModeBrush"] = ModeLight.Fill = color;

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
            for (int i = 0; i < 100; i++)
                Elements[i].Fill = IsPhantom(i)? SolidColorBrush.Parse("Transparent") : Elements[i].Stroke;
        }

        void Update_LaunchpadModel() {
            bool is10x10 = Preferences.LaunchpadModel == LaunchpadModels.Pro || Preferences.LaunchpadModel == LaunchpadModels.All;
            int buttons = is10x10? 10 : 9;
            
            Grid?.Children.Clear();

            IEnumerable<string> ones = Enumerable.Range(0, buttons).Select(i => "*");
            IEnumerable<string> zeros = Enumerable.Range(0, 10 - buttons).Select(i => "0");

            View.Child = Grid = new Grid() {
                RowDefinitions = RowDefinitions.Parse(
                    String.Join(
                        ",",
                        ones.Concat(zeros).ToArray()
                    )
                ),
                ColumnDefinitions = ColumnDefinitions.Parse(
                    String.Join(
                        ",",
                        zeros.Concat(ones).ToArray()
                    )
                )
            };

            for (int i = 0; i < 100; i++) {
                Grid.Children.Add(Canvases[i]);

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

            ModeLight.Opacity = Convert.ToInt32(ModeLight.IsHitTestVisible = is10x10);

            Update_LaunchpadStyle();
        }

        Geometry SquareGeometry => Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
            ((double)this.Resources["PadSize"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadThickness"] / 2).ToString()
        ));

        Geometry CircleGeometry => Geometry.Parse(String.Format("M {0},{1} A {2},{2} 180 1 1 {0},{3} A {2},{2} 180 1 1 {0},{1} Z",
            ((double)this.Resources["PadSize"] / 2).ToString(),
            ((double)this.Resources["PadSize"] / 8 + (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadSize"] * 3 / 8 - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadSize"] * 7 / 8 - (double)this.Resources["PadThickness"] / 2).ToString()
        ));

        Geometry CreateCornerGeometry(string format) => Geometry.Parse(String.Format(format,
            ((double)this.Resources["PadSize"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadCut1"] + (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadCut2"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadThickness"] / 2).ToString()
        ));

        static readonly double[] NovationCoordinates = new double[] { 0.916, 0.073, 0.029, 0.96, 0.989, 0.087, 0.48, 0.509, 0.044, 0.713, 0.247, 0.291, 0.684, 0.887, 0.553, 0.873, 0.596, 0.858, 0.625, 0.829, 0.655, 0.538, 0.931, 0.335, 0.727, 0.756, 0.815, 0.349, 0.844, 0.378, 0.407, 0.451, 0.902, 0.524 }; 
        Geometry NovationGeometry => Geometry.Parse(String.Format(
            "F1 M {0},0 L {1},0 C {2},0 0,{2} 0,{1} L 0,{0} C 0,{3} {2},{4} {1},{4} L {0},{4} C {3},{4} {4},{3} {4},{0} L {4},{1} C {4},{2} {3},0 {0},0 Z M {5},{6} L {7},{8} {9},{10} {11},{12} Z M {13},{14} C {15},{16} {17},{18} {19},{20} L {21},{22} {23},{24} {25},{11} {26},{27} C {28},{29} {15},{30} {13},{31} {13},{6} {32},{33} {13},{14} Z",
            NovationCoordinates.Select(i => (i * ((double)this.Resources["NovationSize"])).ToString()).ToArray()
        ));

        Geometry HiddenGeometry => Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
            ((double)this.Resources["HiddenSize"] - (double)this.Resources["PadThickness"] / 2).ToString(),
            ((double)this.Resources["PadThickness"] / 2).ToString()
        ));

        void DrawPath() {
            this.Resources["SquareGeometry"] = SquareGeometry;
            this.Resources["CircleGeometry"] = CircleGeometry;
            this.Resources["NovationGeometry"] = NovationGeometry;
            this.Resources["HiddenGeometry"] = HiddenGeometry;

            Elements[44].Data = CreateCornerGeometry("M {3},{3} L {3},{0} {2},{0} {0},{2} {0},{3} Z");
            Elements[45].Data = CreateCornerGeometry("M {3},{3} L {3},{2} {1},{0} {0},{0} {0},{3} Z");
            Elements[54].Data = CreateCornerGeometry("M {3},{3} L {3},{0} {0},{0} {0},{1} {2},{3} Z");
            Elements[55].Data = CreateCornerGeometry("M {3},{1} L {3},{0} {0},{0} {0},{3} {1},{3} Z");
        }

        void Update_LaunchpadRotation()
            => Root.LayoutTransform = new RotateTransform(Preferences.LaunchpadGridRotation? -45.0 : 0.0);

        public LaunchpadGrid() {
            InitializeComponent();

            Canvases = new Canvas[100];
            Elements = new Path[100];
            for (int i = 0; i < 100; i++) {
                Canvases[i] = new Canvas();
                Canvases[i].Children.Add(Elements[i] = new Path());

                Grid.SetRow(Canvases[i], i / 10);
                Grid.SetColumn(Canvases[i], i % 10);

                AddClass(i, "empty");
            }
            
            DrawPath();

            Preferences.LaunchpadModelChanged += Update_LaunchpadModel;
            Preferences.LaunchpadStyleChanged += Update_LaunchpadStyle;
            Preferences.LaunchpadGridRotationChanged += Update_LaunchpadRotation;

            Update_LaunchpadModel();
            Update_LaunchpadRotation();
            
            Clear();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            PadStarted = null;
            PadFinished = null;
            PadPressed = null;
            PadReleased = null;
            PadModsPressed = null;

            for (int i = 0; i < 100; i++) {
                if (!Canvases[i].Classes.Contains("empty"))
                    Canvases[i].PointerPressed -= MouseDown;

                if (!Elements[i].Classes.Contains("empty"))
                    Elements[i].PointerPressed -= MouseDown;
                
                Canvases[i] = null;
                Elements[i] = null;
            }

            Grid.Children.Clear();

            Preferences.LaunchpadModelChanged -= Update_LaunchpadModel;
            Preferences.LaunchpadStyleChanged -= Update_LaunchpadStyle;
            Preferences.LaunchpadGridRotationChanged -= Update_LaunchpadRotation;
        }

        public void RenderFrame(Frame frame) {
            for (int i = 0; i < 101; i++)
                SetColor(SignalToGrid(i), frame.Screen[i].ToScreenBrush());
        }

        bool mouseHeld = false;
        IControl mouseOver = null;

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

        void MouseEnter(IControl control, KeyModifiers mods) {
            int index = Array.IndexOf(Canvases, control);
            PadPressed?.Invoke(index);
            PadModsPressed?.Invoke(index, mods);
        }

        void MouseLeave(IControl control) => PadReleased?.Invoke(Array.IndexOf(Canvases, control));

        void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                IInputElement _over = Root.InputHitTest(e.GetPosition(Root));

                if (_over is Shape overPath && !(_over is Rectangle))
                    _over = overPath.Parent;
                
                if (_over is Canvas || _over is Rectangle) {
                    IControl over = (IControl)_over;

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
