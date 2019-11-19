using System;
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

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Hue = this.Get<Dial>("Hue");
            SaturationHigh = this.Get<Dial>("SaturationHigh");
            SaturationLow = this.Get<Dial>("SaturationLow");
            ValueHigh = this.Get<Dial>("ValueHigh");
            ValueLow = this.Get<Dial>("ValueLow");
        }
        
        Tone _tone;

        Dial Hue, SaturationHigh, SaturationLow, ValueHigh, ValueLow;

        public ToneViewer() => new InvalidOperationException();

        public ToneViewer(Tone tone) {
            InitializeComponent();

            _tone = tone;

            Hue.RawValue = _tone.Hue;
            SaturationHigh.RawValue = _tone.SaturationHigh * 100;
            SaturationLow.RawValue = _tone.SaturationLow * 100;
            ValueHigh.RawValue = _tone.ValueHigh * 100;
            ValueLow.RawValue = _tone.ValueLow * 100;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _tone = null;

        void Hue_Changed(Dial sender, double value, double? old) {
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

        void SaturationHigh_Changed(Dial sender, double value, double? old) {
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

        void SaturationLow_Changed(Dial sender, double value, double? old) {
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

        void ValueHigh_Changed(Dial sender, double value, double? old) {
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

        void ValueLow_Changed(Dial sender, double value, double? old) {
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
