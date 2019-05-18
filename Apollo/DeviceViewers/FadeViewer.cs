using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using GradientStop = Avalonia.Media.GradientStop;
using LinearGradientBrush = Avalonia.Media.LinearGradientBrush;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class FadeViewer: UserControl {
        public static readonly string DeviceIdentifier = "fade";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Fade _fade;
        
        Dial Duration, Gate;
        ComboBox PlaybackMode;
        Canvas canvas;
        Grid PickerContainer;
        ColorPicker Picker;
        LinearGradientBrush Gradient;

        List<FadeThumb> thumbs = new List<FadeThumb>();

        int? current;

        public FadeViewer(Fade fade) {
            InitializeComponent();

            _fade = fade;
            _fade.Generated += Gradient_Generate;
            
            canvas = this.Get<Canvas>("Canvas");
            PickerContainer = this.Get<Grid>("PickerContainer");
            Picker = this.Get<ColorPicker>("Picker");
            Gradient = this.Get<LinearGradientBrush>("Gradient");

            PlaybackMode = this.Get<ComboBox>("PlaybackMode");
            PlaybackMode.SelectedItem = _fade.PlayMode;

            Duration = this.Get<Dial>("Duration");
            Duration.UsingSteps = _fade.Mode;
            Duration.Length = _fade.Length;
            Duration.RawValue = _fade.Time;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_fade.Gate * 100;
            
            thumbs.Add(this.Get<FadeThumb>("ThumbStart"));

            for (int i = 1; i < _fade.Count - 1; i++) {
                FadeThumb thumb = new FadeThumb();
                thumbs.Insert(i, thumb);
                Canvas.SetLeft(thumb, (double)_fade.GetPosition(i) * 186 - 7);

                thumb.Moved += Thumb_Move;
                thumb.Focused += Thumb_Focus;
                thumb.Deleted += Thumb_Delete;

                canvas.Children.Add(thumb);
            }

            thumbs.Add(this.Get<FadeThumb>("ThumbEnd"));

            for (int i = 0; i < _fade.Count; i++)
                thumbs[i].Fill = _fade.GetColor(i).ToBrush();

            Gradient_Generate();
        }

        private void Canvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left && e.ClickCount == 2) {
                FadeThumb thumb = new FadeThumb();
                int index;
                double x_center = e.Device.GetPosition(canvas).X;
                double x_left = x_center - 7;

                for (index = 0; index < thumbs.Count; index++) {
                    if (x_left < Canvas.GetLeft(thumbs[index])) {
                        thumbs.Insert(index, thumb);
                        break;
                    }
                }

                Canvas.SetLeft(thumb, x_left);
                thumb.Moved += Thumb_Move;
                thumb.Focused += Thumb_Focus;
                thumb.Deleted += Thumb_Delete;

                _fade.Insert(index, new Color(), (decimal)x_center / 186);
                canvas.Children.Add(thumb);

                if (current != null && index <= current) current++;
                Expand(index);
            }
        }

        private void Expand(int? index) {
            if (current != null) {
                PickerContainer.ColumnDefinitions[0].Width = new GridLength(0);
                thumbs[current.Value].Unselect();

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                PickerContainer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                thumbs[index.Value].Select();

                Picker.SetColor(_fade.GetColor(index.Value));
            }
            
            current = index;
        }

        private void Thumb_Move(FadeThumb sender, VectorEventArgs e) {
            int i = thumbs.IndexOf(sender);

            double left = Canvas.GetLeft(thumbs[i - 1]) + 1;
            double right = Canvas.GetLeft(thumbs[i + 1]) - 1;

            double old = Canvas.GetLeft(sender);
            double x = old + e.Vector.X;

            x = (x < left)? left : x;
            x = (x > right)? right : x;

            _fade.SetPosition(i, (decimal)x / 186);
            Canvas.SetLeft(sender, x);
        }

        private void Thumb_Focus(FadeThumb sender) => Expand(thumbs.IndexOf(sender));

        private void Thumb_Delete(FadeThumb sender) {
            int index = thumbs.IndexOf(sender);

            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }

            thumbs.Remove(sender);
            _fade.Remove(index);
            canvas.Children.Remove(sender);
        }

        private void Color_Changed(Color color, Color old) {
            if (current != null) {
                if (old != null) {
                    Color u = old.Clone();
                    Color r = color.Clone();
                    int index = current.Value;
                    List<int> path = Track.GetPath(_fade);

                    Program.Project.Undo.Add($"Fade Color {current} Changed", () => {
                        ((Fade)Track.TraversePath(path)).SetColor(index, u.Clone());
                    }, () => {
                        ((Fade)Track.TraversePath(path)).SetColor(index, r.Clone());
                    });
                }

                _fade.SetColor(current.Value, color);
            }
        }

        public void SetColor(int index, Color color) {
            if (current == index)
                Picker.SetColor(color);

            thumbs[index].Fill = color.ToBrush();
        }

        private void Gradient_Generate() {
            Gradient.GradientStops.Clear();

            for (int i = 0; i < _fade.Count; i++)
                Gradient.GradientStops.Add(new GradientStop(_fade.GetColor(i).ToAvaloniaColor(), (double)_fade.GetPosition(i)));
        }

        private void Duration_Changed(double value, double? old) {
            if (old != null) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Changed", () => {
                    ((Fade)Track.TraversePath(path)).Time = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Time = r;
                });
            }

            _fade.Time = (int)value;
        }

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        private void Duration_ModeChanged(bool value, bool? old) {
            if (old != null) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Switched", () => {
                    ((Fade)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Mode = r;
                });
            }

            _fade.Mode = value;
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        private void Duration_StepChanged(int value, int? old) {
            if (old != null) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Changed", () => {
                    ((Fade)Track.TraversePath(path)).Length.Step = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Length.Step = r;
                });
            }
        }

        public void SetDurationStep(int duration) => Duration.DrawArcAuto();

        private void Gate_Changed(double value, double? old) {
            if (old != null) {
                decimal u = (decimal)(old.Value / 100);
                decimal r = (decimal)(value / 100);
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Gate Changed", () => {
                    ((Fade)Track.TraversePath(path)).Gate = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Gate = r;
                });
            }

            _fade.Gate = (decimal)(value / 100);
        }

        public void SetGate(decimal gate) => Gate.RawValue = (double)gate * 100;

        private void PlaybackMode_Changed(object sender, SelectionChangedEventArgs e) {
            string selected = (string)PlaybackMode.SelectedItem;

            if (_fade.PlayMode != selected) {
                string u = _fade.PlayMode;
                string r = selected;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Playback Mode Changed", () => {
                    ((Fade)Track.TraversePath(path)).PlayMode = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).PlayMode = r;
                });

                _fade.PlayMode = selected;
            }
        }

        public void SetPlaybackMode(string mode) => PlaybackMode.SelectedItem = mode;
    }
}
