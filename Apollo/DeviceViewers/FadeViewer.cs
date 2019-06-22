using System.Collections.Generic;
using System.Linq;

using Avalonia;
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

        public void Contents_Insert(int index, Color color) {
            FadeThumb thumb = new FadeThumb();
            thumbs.Insert(index, thumb);
            Canvas.SetLeft(thumb, (double)_fade.GetPosition(index) * 186);

            thumb.Moved += Thumb_Move;
            thumb.Focused += Thumb_Focus;
            thumb.Deleted += Thumb_Delete;
            thumb.Fill = color.ToBrush();
            
            canvas.Children.Add(thumb);
            if (current != null && index <= current) current++;
        }

        public void Contents_Remove(int index) {
            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }

            canvas.Children.Remove(thumbs[index]);
            thumbs.RemoveAt(index);
        }

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
            Duration.UsingSteps = _fade.Time.Mode;
            Duration.Length = _fade.Time.Length;
            Duration.RawValue = _fade.Time.Free;

            Gate = this.Get<Dial>("Gate");
            Gate.RawValue = (double)_fade.Gate * 100;
            
            thumbs.Add(this.Get<FadeThumb>("ThumbStart"));
            thumbs[0].Fill = _fade.GetColor(0).ToBrush();

            for (int i = 1; i < _fade.Count - 1; i++) 
                Contents_Insert(i, _fade.GetColor(i));

            thumbs.Add(this.Get<FadeThumb>("ThumbEnd"));
            thumbs.Last().Fill = _fade.GetColor(_fade.Count - 1).ToBrush();

            Gradient_Generate();
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _fade = null;

        public void Expand(int? index) {
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

        private void Canvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left && e.ClickCount == 2) {
                int index;
                double x = e.Device.GetPosition(canvas).X - 7;

                for (index = 0; index < thumbs.Count; index++)
                    if (x < Canvas.GetLeft(thumbs[index])) break;
                
                decimal pos = (decimal)x / 186;

                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Color {index + 1} Inserted", () => {
                    ((Fade)Track.TraversePath(path)).Remove(index);
                }, () => {
                    ((Fade)Track.TraversePath(path)).Insert(index, new Color(), pos);
                });

                _fade.Insert(index, new Color(), pos);
            }
        }

        private void Thumb_Delete(FadeThumb sender) {
            int index = thumbs.IndexOf(sender);

            Color uc = _fade.GetColor(index).Clone();
            decimal up = _fade.GetPosition(index);
            List<int> path = Track.GetPath(_fade);

            Program.Project.Undo.Add($"Fade Color {index + 1} Removed", () => {
                ((Fade)Track.TraversePath(path)).Insert(index, uc, up);
            }, () => {
                ((Fade)Track.TraversePath(path)).Remove(index);
            });

            _fade.Remove(index);
        }

        private void Thumb_Move(FadeThumb sender, double change, double? total) {
            int i = thumbs.IndexOf(sender);

            double left = Canvas.GetLeft(thumbs[i - 1]) + 1;
            double right = Canvas.GetLeft(thumbs[i + 1]) - 1;

            double old = Canvas.GetLeft(sender);
            double x = old + change;

            x = (x < left)? left : x;
            x = (x > right)? right : x;

            decimal pos = (decimal)x / 186;

            if (total != null) {
                double u = x - total.Value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Color {i + 1} Moved", () => {
                    ((Fade)Track.TraversePath(path)).SetPosition(i, (decimal)u / 186);
                }, () => {
                    ((Fade)Track.TraversePath(path)).SetPosition(i, pos);
                });
            }

            _fade.SetPosition(i, pos);
        }

        public void SetPosition(int index, decimal position) => Canvas.SetLeft(thumbs[index], (double)position * 186);

        private void Thumb_Focus(FadeThumb sender) => Expand(thumbs.IndexOf(sender));

        private void Color_Changed(Color color, Color old) {
            if (current != null) {
                if (old != null) {
                    Color u = old.Clone();
                    Color r = color.Clone();
                    int index = current.Value;
                    List<int> path = Track.GetPath(_fade);

                    Program.Project.Undo.Add($"Fade Color {current + 1} Changed to {r.ToHex()}", () => {
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
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Changed to {r}{Duration.Unit}", () => {
                    ((Fade)Track.TraversePath(path)).Time.Free = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Time.Free = r;
                });
            }

            _fade.Time.Free = (int)value;
        }

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        private void Duration_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Switched to {(r? "Steps" : "Free")}", () => {
                    ((Fade)Track.TraversePath(path)).Time.Mode = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Time.Mode = r;
                });
            }

            _fade.Time.Mode = value;
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        private void Duration_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Changed to {Length.Steps[r]}", () => {
                    ((Fade)Track.TraversePath(path)).Time.Length.Step = u;
                }, () => {
                    ((Fade)Track.TraversePath(path)).Time.Length.Step = r;
                });
            }
        }

        public void SetDurationStep(Length duration) => Duration.Length = duration;

        private void Gate_Changed(double value, double? old) {
            if (old != null && old != value) {
                decimal u = (decimal)(old.Value / 100);
                decimal r = (decimal)(value / 100);
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Gate Changed to {value}{Gate.Unit}", () => {
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

                Program.Project.Undo.Add($"Fade Playback Mode Changed to {selected}", () => {
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
