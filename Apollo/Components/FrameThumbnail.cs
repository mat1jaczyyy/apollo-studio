using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Apollo.Structures;

namespace Apollo.Components {
    public class FrameThumbnail: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Time = this.Get<TextBlock>("Time");
            FrameImage = this.Get<Image>("FrameImage");
        }

        public TextBlock Time;
        Image FrameImage;

        Frame _frame = new Frame();
        public Frame Frame {
            get => _frame;
            set {
                _frame = value;
                Time.Text = _frame.ToString();
                Draw();
            }
        }

        public FrameThumbnail() => InitializeComponent();
        
        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _frame = null;
        
        public unsafe void Draw() {
            WriteableBitmap bitmap = new WriteableBitmap(new PixelSize(10, 10), new Vector(96, 96), PixelFormat.Rgba8888);
            
            using (ILockedFramebuffer l = bitmap.Lock()) {
                uint* start = (uint*)l.Address;
                
                for (int y = 0; y < 10; y++) {
                    for (int x = 0; x < 10; x++) {
                        if (x == 0 && y == 0 || x == 9 && y == 0 || x == 0 && y == 9 || x == 9 && y == 9) {} else {
                            start[x + 10 * (9 - y)] = _frame.Screen[x + y * 10].ToUInt32();
                        }
                    }
                }
                
                FrameImage.Source = bitmap;
            }
        }
    }
}
