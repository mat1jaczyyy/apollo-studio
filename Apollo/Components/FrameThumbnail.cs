using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Apollo.Core;
using Apollo.Enums;
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

        public FrameThumbnail() {
            InitializeComponent();
            
            Preferences.LaunchpadModelChanged += Draw;
        }
        
        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            _frame = null;

            Preferences.LaunchpadModelChanged -= Draw;
        }
        
        public unsafe void Draw() {
            WriteableBitmap bitmap = new WriteableBitmap(new PixelSize(10, 10), new Vector(96, 96), PixelFormat.Rgba8888);
            
            using (ILockedFramebuffer l = bitmap.Lock()) {
                uint* start = (uint*)l.Address;

                for (int y = 0; y < 10; y++) {
                    for (int x = 0; x < 10; x++) {
                        int i = y * 10 + x;
                
                        switch (Preferences.LaunchpadModel) {
                            case LaunchpadModels.MK2:
                                if (x == 0 || y == 0 || i == 99) continue;
                                break;

                            case LaunchpadModels.Pro:
                                if (i == 0 || i == 9 || i == 90 || i == 99) continue;
                                break;

                            case LaunchpadModels.X:
                                if (x == 0 || y == 0) continue;
                                break;

                            case LaunchpadModels.ProMK3:
                                if (i == 0 || i == 9) continue;
                                break;
                            
                            case LaunchpadModels.Matrix:
                                if (x == 0 || y == 0 || x == 9 || y == 9) continue;
                                break;
                        }

                        start[(9 - y) * 10 + x] = _frame.Screen[i].ToUInt32();
                    }
                }
                
                FrameImage.Source = bitmap;
            }
        }
    }
}
