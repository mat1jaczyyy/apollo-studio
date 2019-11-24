using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Components;

namespace Apollo.DeviceViewers {
    public class RotateViewer: UserControl {
        public static readonly string DeviceIdentifier = "rotate";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Bypass = this.Get<CheckBox>("Bypass");
            
            AngleDial = this.Get<Dial>("AngleDial");
        }
        
        Rotate _rotate;
        CheckBox Bypass;
        Dial AngleDial;

        public RotateViewer() => new InvalidOperationException();

        public RotateViewer(Rotate rotate) {
            InitializeComponent();

            _rotate = rotate;

            Bypass.IsChecked = _rotate.Bypass;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _rotate = null;

        void Bypass_Changed(object sender, RoutedEventArgs e) {
            bool value = Bypass.IsChecked.Value;

            if (_rotate.Bypass != value) {
                bool u = _rotate.Bypass;
                bool r = value;
                List<int> path = Track.GetPath(_rotate);

                Program.Project.Undo.Add($"Rotate Bypass Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Rotate)Track.TraversePath(path)).Bypass = u;
                }, () => {
                    ((Rotate)Track.TraversePath(path)).Bypass = r;
                });

                _rotate.Bypass = value;
            }
        }

        public void SetBypass(bool value) => Bypass.IsChecked = value;
        
        void Angle_Changed(Dial sender, double angle, double? old){
            if(_rotate.Angle != angle && old != null){
                double o = old.Value/180*Math.PI;
                double a = angle/180*Math.PI;
                
                List<int> path = Track.GetPath(_rotate);
                
                Program.Project.Undo.Add($"Rotate Angle Changed to {angle}°", () => {
                    ((Rotate)Track.TraversePath(path)).Angle = o;
                }, () => {
                    ((Rotate)Track.TraversePath(path)).Angle = a;
                });
            }
            
            _rotate.Angle = angle/180*Math.PI;
        }
        
        public void SetAngle(double angle){
            AngleDial.RawValue = angle/Math.PI*180;
        }
    }
}
