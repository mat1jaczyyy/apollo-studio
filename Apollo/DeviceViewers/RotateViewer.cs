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
    public class RotateViewer: UserControl {
        public static readonly string DeviceIdentifier = "rotate";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            RotateMode = this.Get<ComboBox>("RotateMode");
            Bypass = this.Get<CheckBox>("Bypass");
        }
        
        Rotate _rotate;
        ComboBox RotateMode;
        CheckBox Bypass;

        public RotateViewer() => new InvalidOperationException();

        public RotateViewer(Rotate rotate) {
            InitializeComponent();

            _rotate = rotate;

            RotateMode.SelectedIndex = (int)_rotate.Mode;
            Bypass.IsChecked = _rotate.Bypass;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _rotate = null;

        void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            RotateType selected = (RotateType)RotateMode.SelectedIndex;

            if (_rotate.Mode != selected) {
                RotateType u = _rotate.Mode;
                RotateType r = selected;
                List<int> path = Track.GetPath(_rotate);

                Program.Project.Undo.Add($"Rotate Angle Changed to {((ComboBoxItem)RotateMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    Track.TraversePath<Rotate>(path).Mode = u;
                }, () => {
                    Track.TraversePath<Rotate>(path).Mode = r;
                });

                _rotate.Mode = selected;
            }
        }

        public void SetMode(RotateType mode) => RotateMode.SelectedIndex = (int)mode;

        void Bypass_Changed(object sender, RoutedEventArgs e) {
            bool value = Bypass.IsChecked.Value;

            if (_rotate.Bypass != value) {
                bool u = _rotate.Bypass;
                bool r = value;
                List<int> path = Track.GetPath(_rotate);

                Program.Project.Undo.Add($"Rotate Bypass Changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Rotate>(path).Bypass = u;
                }, () => {
                    Track.TraversePath<Rotate>(path).Bypass = r;
                });

                _rotate.Bypass = value;
            }
        }

        public void SetBypass(bool value) => Bypass.IsChecked = value;
    }
}
