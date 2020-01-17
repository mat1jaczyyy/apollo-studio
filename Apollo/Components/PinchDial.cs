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
        
        protected override void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            Input = this.Get<TextBox>("Input");
        }
        
        public PinchDial() {
            InitializeComponent();
            
            Minimum = -2;
            Maximum = 2;
            Round = 1;
            
            IsBilateral = false;
            
            Arc.StrokeThickness = 2 * Scale;
            
            ArcBase.StrokeThickness = 4 * Scale;
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
        
        void DrawArcBilateral() {
            Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeExtraBrush");

            double realWidthHalf = width * Scale / 2;
            
            Arc.Data = ArcBase.Data = Geometry.Parse(String.Format("M 0 {4} C {0} {1} {2} {3} {4} 0",
                (realWidthHalf + (int)Math.Round(-RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf + (int)Math.Round(-RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf + (int)Math.Round(RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf + (int)Math.Round(RawValue * realWidthHalf / 2)).ToString(),
                (realWidthHalf * 2).ToString()
            )); 
        }
        
        void DrawArcQuad() {
            Arc.Stroke = (IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush");
            
            double realWidth = width * Scale;
            
            Arc.Data = ArcBase.Data = Geometry.Parse(String.Format("M 0 {0} Q {1} {1} {0} 0", 
                realWidth.ToString(),
                (realWidth / 2 + Math.Round(-RawValue * realWidth / 4)).ToString()
            ));
        }
    }
}