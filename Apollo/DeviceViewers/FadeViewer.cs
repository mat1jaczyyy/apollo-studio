using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class FadeViewer: UserControl {
        public static readonly string DeviceIdentifier = "fade";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Fade _fade;
        Canvas canvas;
        Grid PickerContainer;
        ColorPicker Picker;

        List<FadeThumb> thumbs = new List<FadeThumb>();

        int? current;

        public FadeViewer(Fade fade) {
            InitializeComponent();

            _fade = fade;
            
            canvas = this.Get<Canvas>("Canvas");
            PickerContainer = this.Get<Grid>("PickerContainer");
            Picker = this.Get<ColorPicker>("Picker");
            
            thumbs.Add(this.Get<FadeThumb>("ThumbStart"));
            thumbs.Add(this.Get<FadeThumb>("ThumbEnd"));
        }

        private void Canvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left && e.ClickCount == 2) {
                FadeThumb thumb = new FadeThumb();
                int index;
                double x_center = e.Device.GetPosition(canvas).X;
                double x_left = x_center - 6;

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

                _fade.Insert(index, new Color(), (Decimal)x_center / 188);

                canvas.Children.Add(thumb);

                if (current != null && index <= current) current++;
                Expand(index);
            }
        }

        private void Expand(int? index) {
            if (current != null) {
                PickerContainer.ColumnDefinitions[0].Width = new GridLength(0);

                // FadeThumb.UnfocusUI

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                PickerContainer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                Picker.SetColor(_fade.GetColor(index.Value));

                // FadeThumb.FocusUI
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

            Canvas.SetLeft(sender, x);

            _fade.SetPosition(i, (Decimal)x / 188);
        }

        private void Thumb_Focus(FadeThumb sender) => Expand(thumbs.IndexOf(sender));

        private void Thumb_Delete(FadeThumb sender) {
            int index = thumbs.IndexOf(sender);

            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }

            thumbs.Remove(sender);
            canvas.Children.Remove(sender);
        }

        private void Color_Changed(Color color) {
            if (current != null) {
                _fade.SetColor(current.Value, color);
                thumbs[current.Value].Fill = color.ToBrush();
            }
        }

        private void Duration_Changed(double value) => _fade.Time = (int)value;
    }
}
