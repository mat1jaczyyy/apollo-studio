using System;
using System.Linq;

using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Apollo.Themes {
    public class Common: Style {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        static readonly double[] NovationCoordinates = new double[] { 0.916, 0.073, 0.029, 0.96, 0.989, 0.087, 0.48, 0.509, 0.044, 0.713, 0.247, 0.291, 0.684, 0.887, 0.553, 0.873, 0.596, 0.858, 0.625, 0.829, 0.655, 0.538, 0.931, 0.335, 0.727, 0.756, 0.815, 0.349, 0.844, 0.378, 0.407, 0.451, 0.902, 0.524 }; 
        
        double GetResource(string name) => (double)this.Resources[$"LPGrid_{name}"];
        void SetResource(string name, Geometry data) => this.Resources[$"LPGrid_{name}"] = data;

        Geometry CreateCornerGeometry(string format) => Geometry.Parse(String.Format(format,
            (GetResource("PadSize") - GetResource("PadThickness") / 2).ToString(),
            (GetResource("PadCut1") + GetResource("PadThickness") / 2).ToString(),
            (GetResource("PadCut2") - GetResource("PadThickness") / 2).ToString(),
            (GetResource("PadThickness") / 2).ToString()
        ));
        
        public Common() {
            InitializeComponent();
            
            SetResource("SquareGeometry", Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
                (GetResource("PadSize") - GetResource("PadThickness") / 2).ToString(),
                (GetResource("PadThickness") / 2).ToString()
            )));

            SetResource("CircleGeometry", Geometry.Parse(String.Format("M {0},{1} A {2},{2} 180 1 1 {0},{3} A {2},{2} 180 1 1 {0},{1} Z",
                (GetResource("PadSize") / 2).ToString(),
                (GetResource("PadSize") / 8 + GetResource("PadThickness") / 2).ToString(),
                (GetResource("PadSize") * 3 / 8 - GetResource("PadThickness") / 2).ToString(),
                (GetResource("PadSize") * 7 / 8 - GetResource("PadThickness") / 2).ToString()
            )));

            SetResource("NovationGeometry", Geometry.Parse(String.Format(
                "F1 M {0},0 L {1},0 C {2},0 0,{2} 0,{1} L 0,{0} C 0,{3} {2},{4} {1},{4} L {0},{4} C {3},{4} {4},{3} {4},{0} L {4},{1} C {4},{2} {3},0 {0},0 Z M {5},{6} L {7},{8} {9},{10} {11},{12} Z M {13},{14} C {15},{16} {17},{18} {19},{20} L {21},{22} {23},{24} {25},{11} {26},{27} C {28},{29} {15},{30} {13},{31} {13},{6} {32},{33} {13},{14} Z",
                NovationCoordinates.Select(i => (i * (GetResource("NovationSize"))).ToString()).ToArray()
            )));

            SetResource("HiddenGeometry", Geometry.Parse(String.Format("M {1},{1} L {1},{0} {0},{0} {0},{1} Z",
                (GetResource("HiddenSize") - GetResource("PadThickness") / 2).ToString(),
                (GetResource("PadThickness") / 2).ToString()
            )));

            SetResource("44CornerGeometry", CreateCornerGeometry("M {3},{3} L {3},{0} {2},{0} {0},{2} {0},{3} Z"));
            SetResource("45CornerGeometry", CreateCornerGeometry("M {3},{3} L {3},{2} {1},{0} {0},{0} {0},{3} Z"));
            SetResource("54CornerGeometry", CreateCornerGeometry("M {3},{3} L {3},{0} {0},{0} {0},{1} {2},{3} Z"));
            SetResource("55CornerGeometry", CreateCornerGeometry("M {3},{1} L {3},{0} {0},{0} {0},{3} {1},{3} Z"));
        }
    }

    public class Dark: Style {}
    public class Light: Style {}
}