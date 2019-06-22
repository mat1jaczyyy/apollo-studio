using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class ToneViewer: UserControl {
        public static readonly string DeviceIdentifier = "tone";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Tone _tone;

        Dial Hue, SaturationHigh, SaturationLow, ValueHigh, ValueLow;

        public ToneViewer(Tone tone) {
            InitializeComponent();

            _tone = tone;

            Hue = this.Get<Dial>("Hue");
            Hue.RawValue = _tone.Hue;

            SaturationHigh = this.Get<Dial>("SaturationHigh");
            SaturationHigh.RawValue = _tone.SaturationHigh * 100;
            
            SaturationLow = this.Get<Dial>("SaturationLow");
            SaturationLow.RawValue = _tone.SaturationLow * 100;
            
            ValueHigh = this.Get<Dial>("ValueHigh");
            ValueHigh.RawValue = _tone.ValueHigh * 100;
            
            ValueLow = this.Get<Dial>("ValueLow");
            ValueLow.RawValue = _tone.ValueLow * 100;
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _tone = null;

        private void Hue_Changed(double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value;
                double r = value;
                List<int> path = Track.GetPath(_tone);

                Program.Project.Undo.Add($"Tone Hue Changed to {r}{Hue.Unit}", () => {
                    ((Tone)Track.TraversePath(path)).Hue = u;
                }, () => {
                    ((Tone)Track.TraversePath(path)).Hue = r;
                });
            }

            _tone.Hue = value;
        }

        public void SetHue(double value) => Hue.RawValue = value;

        private void SaturationHigh_Changed(double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_tone);

                Program.Project.Undo.Add($"Tone Sat Hi Changed to {r}{SaturationHigh.Unit}", () => {
                    ((Tone)Track.TraversePath(path)).SaturationHigh = u;
                }, () => {
                    ((Tone)Track.TraversePath(path)).SaturationHigh = r;
                });
            }

            _tone.SaturationHigh = value / 100;
        }

        public void SetSaturationHigh(double value) => SaturationHigh.RawValue = value * 100;

        private void SaturationLow_Changed(double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_tone);

                Program.Project.Undo.Add($"Tone Sat Lo Changed to {r}{SaturationLow.Unit}", () => {
                    ((Tone)Track.TraversePath(path)).SaturationLow = u;
                }, () => {
                    ((Tone)Track.TraversePath(path)).SaturationLow = r;
                });
            }

            _tone.SaturationLow = value / 100;
        }

        public void SetSaturationLow(double value) => SaturationLow.RawValue = value * 100;

        private void ValueHigh_Changed(double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_tone);

                Program.Project.Undo.Add($"Tone Val Hi Changed to {r}{ValueHigh.Unit}", () => {
                    ((Tone)Track.TraversePath(path)).ValueHigh = u;
                }, () => {
                    ((Tone)Track.TraversePath(path)).ValueHigh = r;
                });
            }

            _tone.ValueHigh = value / 100;
        }

        public void SetValueHigh(double value) => ValueHigh.RawValue = value * 100;

        private void ValueLow_Changed(double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_tone);

                Program.Project.Undo.Add($"Tone Val Lo Changed to {r}{ValueLow.Unit}", () => {
                    ((Tone)Track.TraversePath(path)).ValueLow = u;
                }, () => {
                    ((Tone)Track.TraversePath(path)).ValueLow = r;
                });
            }

            _tone.ValueLow = value / 100;
        }

        public void SetValueLow(double value) => ValueLow.RawValue = value * 100;
    }
}
