using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Components;
using System.Collections.Generic;
using Apollo.Core;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class BlurViewer: UserControl {
        public static readonly string DeviceIdentifier = "blur";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Radius = this.Get<Dial>("Radius");
            Amount = this.Get<Dial>("Amount");
        }
        
        Blur _blur;
        Dial Radius, Amount;

        public BlurViewer() => new InvalidOperationException();

        public BlurViewer(Blur blur) {
            InitializeComponent();

            _blur = blur;

            Radius.RawValue = _blur.Radius;
            Amount.RawValue = _blur.Amount * 100;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _blur = null;

        public void SetRadius(double radius) => Radius.RawValue = radius;
        void Radius_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                double u = old.Value;
                double r = value;
                List<int> path = Track.GetPath(_blur);

                Program.Project.Undo.Add($"Blur Radius Changed to {value}", () => {
                    Track.TraversePath<Blur>(path).Radius = u;
                }, () => {
                    Track.TraversePath<Blur>(path).Radius = r;
                });
            }

            _blur.Radius = value;
        }
        
        public void SetAmount(double amount) => Amount.RawValue = amount * 100;
        void Amount_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_blur);

                Program.Project.Undo.Add($"Blur Amount Changed to {value}%", () => {
                    Track.TraversePath<Blur>(path).Amount = u;
                }, () => {
                    Track.TraversePath<Blur>(path).Amount = r;
                });
            }

            _blur.Amount = value / 100;
        }
    }
}
