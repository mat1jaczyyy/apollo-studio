using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

using RotateType = Apollo.Devices.Rotate.RotateType;

namespace Apollo.DeviceViewers {
    public class RotateViewer: UserControl {
        public static readonly string DeviceIdentifier = "rotate";

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            RotateMode = this.Get<ComboBox>("RotateMode");
            Bypass = this.Get<CheckBox>("Bypass");
        }
        
        Rotate _rotate;
        ComboBox RotateMode;
        CheckBox Bypass;

        public RotateViewer(Rotate rotate) {
            InitializeComponent();

            _rotate = rotate;

            RotateMode.SelectedIndex = (int)_rotate.Mode;
            Bypass.IsChecked = _rotate.Bypass;
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _rotate = null;

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            RotateType selected = (RotateType)RotateMode.SelectedIndex;

            if (_rotate.Mode != selected) {
                RotateType u = _rotate.Mode;
                RotateType r = selected;
                List<int> path = Track.GetPath(_rotate);

                Program.Project.Undo.Add($"Rotate Angle Changed to {((ComboBoxItem)RotateMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    ((Rotate)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Rotate)Track.TraversePath(path)).Mode = r;
                });

                _rotate.Mode = selected;
            }
        }

        public void SetMode(RotateType mode) => RotateMode.SelectedIndex = (int)mode;

        private void Bypass_Changed(object sender, EventArgs e) {
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
    }
}
