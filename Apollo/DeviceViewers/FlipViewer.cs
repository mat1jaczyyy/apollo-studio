﻿using System;
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
    public class FlipViewer: UserControl {
        public static readonly string DeviceIdentifier = "flip";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            AngleDial = this.Get<Dial>("AngleDial");
            Bypass = this.Get<CheckBox>("Bypass");
        }
        
        Flip _flip;
        ComboBox FlipMode;
        CheckBox Bypass;
        Dial AngleDial;

        public FlipViewer() => new InvalidOperationException();

        public FlipViewer(Flip flip) {
            InitializeComponent();

            _flip = flip;
            
            Bypass.IsChecked = _flip.Bypass;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _flip = null;

        void Angle_Changed(Dial sender, double angle, double? old) {
            if (old != null && old != angle) {
                double a = angle;
                double o = old.Value;
                List<int> path = Track.GetPath(_flip);

                Program.Project.Undo.Add($"Flip Angle Changed to {a}°", () => {
                    ((Flip)Track.TraversePath(path)).Angle = o / 180 * Math.PI;
                }, () => {
                    ((Flip)Track.TraversePath(path)).Angle = a / 180 * Math.PI;
                });

                _flip.Angle = a / 180 * Math.PI;
            }
        }

        public void SetAngle(double angle) => AngleDial.RawValue = angle / Math.PI * 180;

        void Bypass_Changed(object sender, RoutedEventArgs e) {
            bool value = Bypass.IsChecked.Value;

            if (_flip.Bypass != value) {
                bool u = _flip.Bypass;
                bool r = value;
                List<int> path = Track.GetPath(_flip);

                Program.Project.Undo.Add($"Flip Bypass Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Flip)Track.TraversePath(path)).Bypass = u;
                }, () => {
                    ((Flip)Track.TraversePath(path)).Bypass = r;
                });

                _flip.Bypass = value;
            }
        }

        public void SetBypass(bool value) => Bypass.IsChecked = value;
    }
}
