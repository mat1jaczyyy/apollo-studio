using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GradientStop = Avalonia.Media.GradientStop;
using IBrush = Avalonia.Media.IBrush;
using LinearGradientBrush = Avalonia.Media.LinearGradientBrush;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class FadeViewer: UserControl {
        public static readonly string DeviceIdentifier = "fade";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            canvas = this.Get<Canvas>("Canvas");
            PickerContainer = this.Get<Grid>("PickerContainer");
            Picker = this.Get<ColorPicker>("Picker");
            Gradient = this.Get<Rectangle>("Gradient");
            ResizeArea = this.Get<Grid>("ResizeArea");

            PlaybackMode = this.Get<ComboBox>("PlaybackMode");
            Duration = this.Get<Dial>("Duration");
            Gate = this.Get<Dial>("Gate");

            PositionText = this.Get<TextBlock>("PositionText");
            Display = this.Get<TextBlock>("Display");
            Input = this.Get<TextBox>("Input");
        }
        
        IDisposable observable;
        
        Fade _fade;
        
        Dial Duration, Gate;
        ComboBox PlaybackMode;
        TextBlock PositionText, Display;
        TextBox Input;
        Canvas canvas;
        Grid PickerContainer, ResizeArea;
        ColorPicker Picker;
        Rectangle Gradient;

        List<FadeThumb> thumbs = new List<FadeThumb>();
        List<Fade.FadeInfo> fullFade;

        public void Contents_Insert(int index, Color color) {
            FadeThumb thumb = new FadeThumb() {
                Owner = this,
                Fill = color.ToBrush()
            };
            thumbs.Insert(index, thumb);
            Canvas.SetLeft(thumb, _fade.GetPosition(index) * Gradient.Width);

            thumb.Moved += Thumb_Move;
            thumb.Focused += Thumb_Focus;
            thumb.Deleted += Thumb_Delete;
            thumb.TypeChanged += Thumb_ChangeFadeType;
            
            canvas.Children.Add(thumb);
            if (_fade.Expanded != null && index <= _fade.Expanded) _fade.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (_fade.Expanded != null) {
                if (index < _fade.Expanded) _fade.Expanded--;
                else if (index == _fade.Expanded) Expand(null);
            }

            canvas.Children.Remove(thumbs[index]);
            thumbs.RemoveAt(index);
        }

        public FadeViewer() => new InvalidOperationException();

        public FadeViewer(Fade fade) {
            InitializeComponent();

            _fade = fade;
            _fade.Generated += Gradient_Generate;

            PlaybackMode.SelectedIndex = (int)_fade.PlayMode;

            Duration.UsingSteps = _fade.Time.Mode;
            Duration.Length = _fade.Time.Length;
            Duration.RawValue = _fade.Time.Free;

            Gate.RawValue = _fade.Gate * 100;

            int? temp = _fade.Expanded;
            _fade.Expanded = null;
            
            FadeThumb ThumbStart, ThumbEnd;

            thumbs.Add(ThumbStart = this.Get<FadeThumb>("ThumbStart"));
            ThumbStart.Fill = _fade.GetColor(0).ToBrush();

            for (int i = 1; i < _fade.Count - 1; i++)
                Contents_Insert(i, _fade.GetColor(i));

            thumbs.Add(ThumbEnd = this.Get<FadeThumb>("ThumbEnd"));
            ThumbEnd.Fill = _fade.GetColor(_fade.Count - 1).ToBrush();

            ThumbStart.Owner = ThumbEnd.Owner = this;

            Expand(temp);

            observable = Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);

            _fade.Generate();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            _fade.Generated -= Gradient_Generate;
            _fade = null;

            observable.Dispose();
        }

        public void Expand(int? index) {
            if (_fade.Expanded != null) {
                PickerContainer.MaxWidth = 0;
                thumbs[_fade.Expanded.Value].Unselect();

                PositionText.Text = "";
                Display.Text = "";

                if (index == _fade.Expanded) {
                    _fade.Expanded = null;
                    return;
                }
            }

            if (index != null) {
                PickerContainer.MaxWidth = double.PositiveInfinity;
                thumbs[index.Value].Select();

                if (index != 0 && index != _fade.Count - 1) {
                    PositionText.Text = "Position:";
                    Display.Text = $"{(Math.Round(_fade.GetPosition(index.Value) * 1000) / 10).ToString()}%";
                }

                Picker.SetColor(_fade.GetColor(index.Value));
            }
            
            _fade.Expanded = index;
        }

        void Canvas_MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed && e.ClickCount == 2) {
                int index;
                double x = e.GetPosition(canvas).X - 7;

                for (index = 0; index < thumbs.Count; index++)
                    if (x < Canvas.GetLeft(thumbs[index])) break;
                
                double pos = x / Gradient.Width;
                
                double time = pos * _fade.Time;
                
                int fadeIndex = 0;
                for (int i = 0; i < fullFade.Count; i++)
                    if (Math.Abs(fullFade[i].Time - time) < Math.Abs(fullFade[fadeIndex].Time - time))
                        fadeIndex = i;
                
                Color thumbColor = fullFade[fadeIndex].Color.Clone();
                
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Color {index + 1} Inserted", () => {
                    Track.TraversePath<Fade>(path).Remove(index);
                }, () => {
                    Track.TraversePath<Fade>(path).Insert(index, thumbColor, pos, FadeType.Linear);
                });

                _fade.Insert(index, thumbColor.Clone(), pos, FadeType.Linear);
            }
        }

        void Thumb_Delete(FadeThumb sender) {
            int index = thumbs.IndexOf(sender);

            Color uc = _fade.GetColor(index).Clone();
            double up = _fade.GetPosition(index);
            FadeType ut = _fade.GetFadeType(index);
            List<int> path = Track.GetPath(_fade);

            Program.Project.Undo.Add($"Fade Color {index + 1} Removed", () => {
                Track.TraversePath<Fade>(path).Insert(index, uc, up, ut);
            }, () => {
                Track.TraversePath<Fade>(path).Remove(index);
            });

            _fade.Remove(index);
        }

        void Thumb_ChangeFadeType(FadeThumb sender, FadeType newType) {
            int index = thumbs.IndexOf(sender);

            FadeType oldType = _fade.GetFadeType(index);

            List<int> path = Track.GetPath(_fade);

            Program.Project.Undo.Add($"Fade Type {index + 1} Changed to {newType}", () => {
                Track.TraversePath<Fade>(path).SetFadeType(index, oldType);
            }, () => {
                Track.TraversePath<Fade>(path).SetFadeType(index, newType);
            });

            _fade.SetFadeType(index, newType);
        }

        public FadeType GetFadeType(FadeThumb sender) => _fade.GetFadeType(thumbs.IndexOf(sender));

        void Thumb_Move(FadeThumb sender, double change, double? total) {
            int i = thumbs.IndexOf(sender);

            double left = Canvas.GetLeft(thumbs[i - 1]) + 1;
            double right = Canvas.GetLeft(thumbs[i + 1]) - 1;

            double old = Canvas.GetLeft(sender);
            double x = old + change;

            x = (x < left)? left : x;
            x = (x > right)? right : x;

            double pos = x / Gradient.Width;

            if (total != null) {
                double u = x - total.Value / Gradient.Width;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Color {i + 1} Moved", () => {
                    Track.TraversePath<Fade>(path).SetPosition(i, u);
                }, () => {
                    Track.TraversePath<Fade>(path).SetPosition(i, pos);
                });
            }

            _fade.SetPosition(i, pos);
        }

        public void SetPosition(int index, double position) {
            Canvas.SetLeft(thumbs[index], position * Gradient.Width);

            if (index == _fade.Expanded)
                Display.Text = $"{(Math.Round(_fade.GetPosition(index) * 1000) / 10).ToString()}%";
        }

        void Thumb_Focus(FadeThumb sender) => Expand(thumbs.IndexOf(sender));

        void Color_Changed(Color color, Color old) {
            if (_fade.Expanded != null) {
                if (old != null) {
                    Color u = old.Clone();
                    Color r = color.Clone();
                    int index = _fade.Expanded.Value;
                    List<int> path = Track.GetPath(_fade);

                    Program.Project.Undo.Add($"Fade Color {_fade.Expanded + 1} Changed to {r.ToHex()}", () => {
                        Track.TraversePath<Fade>(path).SetColor(index, u.Clone());
                    }, () => {
                        Track.TraversePath<Fade>(path).SetColor(index, r.Clone());
                    });
                }

                _fade.SetColor(_fade.Expanded.Value, color);
            }
        }

        public void SetColor(int index, Color color) {
            if (_fade.Expanded == index)
                Picker.SetColor(color);

            thumbs[index].Fill = color.ToBrush();
        }

        void Gradient_Generate(List<Fade.FadeInfo> points) {
            if (Program.Project.IsDisposing) return;
            
            fullFade = points;

            LinearGradientBrush gradient = (LinearGradientBrush)Gradient.Fill;
            
            Dispatcher.UIThread.InvokeAsync(() => {
                gradient.GradientStops.Clear();

                if (_fade == null) return;

                for (int i = 0; i < points.Count; i++) {
                    if (i > 0 && points[i - 1].IsHold)
                        gradient.GradientStops.Add(new GradientStop(points[i - 1].Color.ToAvaloniaColor(), (points[i].Time - .0000000001) / (_fade.Time * _fade.Gate)));

                    gradient.GradientStops.Add(new GradientStop(points[i].Color.ToAvaloniaColor(), points[i].Time / (_fade.Time * _fade.Gate)));
                }
            });
        }

        void Duration_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Changed to {r}{Duration.Unit}", () => {
                    Track.TraversePath<Fade>(path).Time.Free = u;
                }, () => {
                    Track.TraversePath<Fade>(path).Time.Free = r;
                });
            }

            _fade.Time.Free = (int)value;
        }

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        void Duration_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Switched to {(r? "Steps" : "Free")}", () => {
                    Track.TraversePath<Fade>(path).Time.Mode = u;
                }, () => {
                    Track.TraversePath<Fade>(path).Time.Mode = r;
                });
            }

            _fade.Time.Mode = value;
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        void Duration_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Duration Changed to {Length.Steps[r]}", () => {
                    Track.TraversePath<Fade>(path).Time.Length.Step = u;
                }, () => {
                    Track.TraversePath<Fade>(path).Time.Length.Step = r;
                });
            }
        }

        public void SetDurationStep(Length duration) => Duration.Length = duration;

        void Gate_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Gate Changed to {value}{Gate.Unit}", () => {
                    Track.TraversePath<Fade>(path).Gate = u;
                }, () => {
                    Track.TraversePath<Fade>(path).Gate = r;
                });
            }

            _fade.Gate = value / 100;
        }

        public void SetGate(double gate) => Gate.RawValue = gate * 100;

        void PlaybackMode_Changed(object sender, SelectionChangedEventArgs e) {
            FadePlaybackType selected = (FadePlaybackType)PlaybackMode.SelectedIndex;

            if (_fade.PlayMode != selected) {
                FadePlaybackType u = _fade.PlayMode;
                FadePlaybackType r = selected;
                List<int> path = Track.GetPath(_fade);

                Program.Project.Undo.Add($"Fade Playback Mode Changed to {r}", () => {
                    Track.TraversePath<Fade>(path).PlayMode = u;
                }, () => {
                    Track.TraversePath<Fade>(path).PlayMode = r;
                });

                _fade.PlayMode = selected;
            }
        }

        public void SetPlaybackMode(FadePlaybackType mode) => PlaybackMode.SelectedIndex = (int)mode;

        Action Input_Update;

        void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            Input_Update = () => { Input.Text = (Math.Round(_fade.GetPosition(_fade.Expanded.Value) * 1000) / 10).ToString(); };

            if (double.TryParse(text, out double value)) {
                double min = _fade.GetPosition(_fade.Expanded.Value - 1) * 100 + 0.5;
                double max = _fade.GetPosition(_fade.Expanded.Value + 1) * 100 - 0.5;

                if (min <= value && value <= max) {
                    _fade.SetPosition(_fade.Expanded.Value, value / 100);
                    Input_Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    Input_Update = () => { Input.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                Input_Update += () => {
                    if (value < 0) text = $"-{text.Substring(1).TrimStart('0')}";
                    else if (value > 0) text = text.TrimStart('0');
                    else text = "0";

                    if (value <= 0) text = "0";

                    int upper = (int)Math.Pow(10, ((int)max).ToString().Length) - 1;
                    if (value > upper) text = upper.ToString();
                    
                    Input.Text = text;
                };
            }

            Dispatcher.UIThread.InvokeAsync(() => {
                Input_Update?.Invoke();
                Input_Update = null;
            });
        }

        double oldValue;

        void DisplayPressed(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed && e.ClickCount == 2) {
                oldValue = Math.Round(_fade.GetPosition(_fade.Expanded.Value) * 1000) / 10;
                Input.Text = oldValue.ToString();

                Input.SelectionStart = 0;
                Input.SelectionEnd = Input.Text.Length;
                Input.CaretIndex = Input.Text.Length;

                Input.Opacity = 1;
                Input.IsHitTestVisible = true;
                Input.Focus();

                e.Handled = true;
            }
        }
        
        void Input_LostFocus(object sender, RoutedEventArgs e) {
            double raw = _fade.GetPosition(_fade.Expanded.Value);

            Input.Text = (Math.Round(raw * 1000) / 10).ToString();

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;

            double u = oldValue / 100;
            double r = raw;
            int i = _fade.Expanded.Value;
            List<int> path = Track.GetPath(_fade);

            Program.Project.Undo.Add($"Fade Color {i + 1} Moved", () => {
                Track.TraversePath<Fade>(path).SetPosition(i, u);
            }, () => {
                Track.TraversePath<Fade>(path).SetPosition(i, r);
            });
        }

        void Input_KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        void Input_KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;

        bool mouseHeld;
        double original;

        void ResizeDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) {
                mouseHeld = true;
                e.Pointer.Capture(ResizeArea);

                original = e.GetPosition(ResizeArea).X;

                ResizeMove(sender, e);
            }
        }

        void ResizeUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) {
                ResizeMove(sender, e);

                mouseHeld = false;
                e.Pointer.Capture(null);
            }
        }

        void ResizeMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                double width = Math.Min(1200, Math.Max(170, Gradient.Width + e.GetPosition(ResizeArea).X - original));

                Gradient.Width = width;
                canvas.Width = width + 14;

                for (int i = 0; i < _fade.Count; i++)
                    Canvas.SetLeft(thumbs[i], _fade.GetPosition(i) * Gradient.Width);
            }
        }
    }
}
