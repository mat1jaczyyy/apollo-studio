using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class FlipViewer: UserControl {
        public static readonly string DeviceIdentifier = "flip";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Flip _flip;
        ComboBox FlipMode;
        CheckBox Bypass;

        public FlipViewer(Flip flip) {
            InitializeComponent();

            _flip = flip;

            FlipMode = this.Get<ComboBox>("FlipMode");
            FlipMode.SelectedItem = _flip.Mode;
            
            Bypass = this.Get<CheckBox>("Bypass");
            Bypass.IsChecked = _flip.Bypass;
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _flip = null;

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            string selected = (string)FlipMode.SelectedItem;

            if (_flip.Mode != selected) {
                string u = _flip.Mode;
                string r = selected;
                List<int> path = Track.GetPath(_flip);

                Program.Project.Undo.Add($"Flip Orientation Changed to {selected}", () => {
                    ((Flip)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Flip)Track.TraversePath(path)).Mode = r;
                });

                _flip.Mode = selected;
            }
        }

        public void SetMode(string mode) => FlipMode.SelectedItem = mode;

        private void Bypass_Changed(object sender, EventArgs e) {
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
