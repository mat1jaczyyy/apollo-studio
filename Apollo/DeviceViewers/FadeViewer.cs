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

        List<FadeThumb> thumbs = new List<FadeThumb>();

        public FadeViewer(Fade fade) {
            InitializeComponent();

            _fade = fade;
            
            canvas = this.Get<Canvas>("Canvas");
            
            thumbs.Add(this.Get<FadeThumb>("ThumbStart"));
            thumbs.Add(this.Get<FadeThumb>("ThumbEnd"));
        }

        private void Canvas_MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) {
                FadeThumb thumb = new FadeThumb();
                double x = e.Device.GetPosition(canvas).X - 6;

                for (int i = 0; i < thumbs.Count; i++) {
                    if (x < Canvas.GetLeft(thumbs[i])) {
                        thumbs.Insert(i, thumb);
                        break;
                    }
                }

                Canvas.SetLeft(thumb, e.Device.GetPosition(canvas).X - 6);
                thumb.Moved += Thumb_Move;
                thumb.Deleted += Thumb_Delete;

                canvas.Children.Add(thumb);
            }
        }

        private void Thumb_Move(FadeThumb sender, VectorEventArgs e) {
            double old = Canvas.GetLeft(sender);
            double x = old + e.Vector.X;
            x = (x < 0)? 0 : x;
            x = x > canvas.Bounds.Width? canvas.Bounds.Width : x;

            Canvas.SetLeft(sender, x);
        }

        private void Thumb_Delete(FadeThumb sender) {
            thumbs.Remove(sender);
            canvas.Children.Remove(sender);
        }

        private void Duration_Changed(double value) => _fade.Time = (int)value;
    }
}
