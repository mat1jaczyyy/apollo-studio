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
    public class GraphDial: Dial {
        
        public delegate void DialBilateralChangedEventHandler(bool NewValue, bool? OldValue);
        public event DialBilateralChangedEventHandler BilateralChanged;
        
        bool _isBilateral;
        public bool IsBilateral{
            get => _isBilateral;
            set {
                _isBilateral = value;
                DrawArcAuto();
                BilateralChanged?.Invoke(IsBilateral, !IsBilateral);
            }
        }
        
        protected override void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            Input = this.Get<TextBox>("Input");
        }
        
        public GraphDial(){
            InitializeComponent();
            
            Minimum = -2;
            Maximum = 2;
            Round = 1;
            
            IsBilateral = false;
            
            Arc.StrokeThickness = 2 * Scale;
            
            ModeChanged += (bool _, bool? __) => IsBilateral = !IsBilateral;
        }
        
        public override void DrawArcAuto() {
            if(!Enabled){
                Display.Text = (Enabled || !DisplayDisabledText)? ValueString : DisabledText;
                Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundLowBrush");
                return;
            }
            
            if (IsBilateral) DrawArcBilateral();
            else DrawArcQuad();
        }
        
        protected override void DrawArc(Path Arc, double value, bool overrideBase, string color = "ThemeAccentBrush"){
            DrawArcAuto();
        }
        
        void DrawArcBilateral(){
            Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeExtraBrush");

            double realWidthHalf = width * Scale / 2;
            
            Arc.Data = Geometry.Parse(String.Format("M 0 43 C {0} {1} {2} {3} 43 0",
                (realWidthHalf + (int)Math.Round(-RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf + (int)Math.Round(-RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf + (int)Math.Round(RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf + (int)Math.Round(RawValue * realWidthHalf / 2)).ToString()
            )); 
        }
        
        void DrawArcQuad(){
            Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush");
            
            double realWidth = width * Scale;
            
            Arc.Data = Geometry.Parse(String.Format("M 0 {0} Q {1} {1} {0} 0", 
                realWidth.ToString(),
                (realWidth / 2 + Math.Round(-RawValue * realWidth / 4)).ToString()
            ));
        }
    }
}