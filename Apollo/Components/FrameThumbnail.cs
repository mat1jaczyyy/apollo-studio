using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Apollo.Structures;

using System;

namespace Apollo.Components
{
    public class FrameThumbnail : UserControl
    {

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            Time = this.Get<TextBlock>("Time");
            FrameImage = this.Get<Image>("FrameImage");
        }

        public delegate void ClickedEventHandler();
        public event ClickedEventHandler Clicked;

        Frame _frame = new Frame();
        public TextBlock Time;

        Image FrameImage;

        public Frame Frame
        {
            get => _frame;
            set {
                _frame = value;
                Time.Text = _frame.ToString();
                Draw_Launchpad();
            }
        }

        public FrameThumbnail()
        {
            InitializeComponent();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e)
        {
            Clicked = null;

            _frame = null;
        }
        
        public unsafe void Draw_Launchpad(){
            WriteableBitmap bitmap = new WriteableBitmap(new PixelSize(10, 10), new Vector(96, 96), PixelFormat.Rgba8888);
            
            using(ILockedFramebuffer l = bitmap.Lock()){
                uint* start = (uint*)l.Address;
                
                for(int y = 0; y < 10; y++){
                    for (int x = 0; x < 10; x++){
                        if(x == 0 && y == 0 || x == 9 && y == 0 || x == 0 && y == 9 || x == 9 && y == 9){
                            start[x + 10 * (9 - y)] = 0b_0000_0000 << 24;
                        }else{
                            start[x + 10 * (9 - y)] = _frame.Screen[x + y * 10].ToUInt32();
                        }
                    }
                }
                
                FrameImage.Source = bitmap;
            }
            
        }

        void Click(object sender, PointerReleasedEventArgs e)
        {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) Clicked?.Invoke();
        }
    }
}
