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
    public class PinchDial: Dial {
        public delegate void DialBilateralChangedEventHandler(PinchDial sender, bool NewValue, bool? OldValue);
        public event DialBilateralChangedEventHandler BilateralChanged;
        
        bool _isBilateral;
        public bool IsBilateral {
            get => _isBilateral;
            set {
                _isBilateral = value;
                DrawArcAuto();
                BilateralChanged?.Invoke(this, IsBilateral, !IsBilateral);
            }
        }

        double lineWidth => radius * 1.9;

        public PinchDial() {
            Minimum = -2;
            Maximum = 2;
            Round = 1;
            
            IsBilateral = false;
            
            Arc.StrokeThickness = stroke * Scale / 2;
            
            ArcBase.StrokeThickness = stroke * Scale;
            ArcBase.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundLowBrush");
            
            ModeChanged += (bool _, bool? __) => IsBilateral = !IsBilateral;
        }
        
        protected void Graph_Unloaded(object sender, VisualTreeAttachmentEventArgs e){
            Unloaded(sender, e);
            
            BilateralChanged = null;
        }
        
        public override void DrawArcAuto() {
            Display.Text = (Enabled || !DisplayDisabledText)? ValueString : DisabledText;
            
            if(!Enabled){
                Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundLowBrush");
                ArcBase.IsVisible = false;
                return;
            }
            
            ArcBase.IsVisible = true;
            
            
            if (IsBilateral) DrawArcBilateral();
            else DrawArcQuad();
        }
        
        protected override void DrawArc(Path Arc, double value, bool overrideBase, string color = "ThemeAccentBrush") {
            DrawArcAuto();
        }

        Geometry CreateGeometry(string geometry) {
            double realHeight = height * Scale;
            double margin = (width - height) * Scale / 2;

            return Geometry.Parse(String.Format("M {0} {1} " + geometry + " {6} 0",
                margin.ToString(),
                (realHeight).ToString(),
                (realHeight * (1 - Value) + margin).ToString(),
                (realHeight * (1 - Value)).ToString(),
                (realHeight * Value + margin).ToString(),
                (realHeight * Value).ToString(),
                (realHeight + margin).ToString()
            ));
        }
        
        void DrawArcBilateral() {
            Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeExtraBrush");

            Arc.Data = ArcBase.Data = CreateGeometry("C {2} {3} {4} {5}");
        }
        
        void DrawArcQuad() {
            Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush");
            
            double realWidth = lineWidth * Scale;
            double margin = (width - lineWidth) / 2;
            
            Arc.Data = ArcBase.Data = CreateGeometry("Q {2} {3}");
        }
    }
}