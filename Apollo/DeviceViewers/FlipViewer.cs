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

namespace Apollo.DeviceViewers {
    public class FlipViewer: UserControl {
        public static readonly string DeviceIdentifier = "flip";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            FlipMode = this.Get<ComboBox>("FlipMode");
            Bypass = this.Get<CheckBox>("Bypass");
        }
        
        Flip _flip;
        ComboBox FlipMode;
        CheckBox Bypass;

        public FlipViewer() => new InvalidOperationException();

        public FlipViewer(Flip flip) {
            InitializeComponent();

            _flip = flip;

            FlipMode.SelectedIndex = (int)_flip.Mode;
            Bypass.IsChecked = _flip.Bypass;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _flip = null;

        void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            FlipType selected = (FlipType)FlipMode.SelectedIndex;

            if (_flip.Mode != selected) {
                FlipType u = _flip.Mode;
                FlipType r = selected;
                List<int> path = Track.GetPath(_flip);

                Program.Project.Undo.Add($"Flip Orientation Changed to {((ComboBoxItem)FlipMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    Track.TraversePath<Flip>(path).Mode = u;
                }, () => {
                    Track.TraversePath<Flip>(path).Mode = r;
                });

                _flip.Mode = selected;
            }
        }

        public void SetMode(FlipType mode) => FlipMode.SelectedIndex = (int)mode;

        void Bypass_Changed(object sender, RoutedEventArgs e) {
            bool value = Bypass.IsChecked.Value;

            if (_flip.Bypass != value) {
                bool u = _flip.Bypass;
                bool r = value;
                List<int> path = Track.GetPath(_flip);

                Program.Project.Undo.Add($"Flip Bypass Changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Flip>(path).Bypass = u;
                }, () => {
                    Track.TraversePath<Flip>(path).Bypass = r;
                });

                _flip.Bypass = value;
            }
        }

        public void SetBypass(bool value) => Bypass.IsChecked = value;
    }
}
