using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace api {
    public class Splash: Window {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public Splash() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            this.Get<Image>("img").Source = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("api.Resources.SplashImage.png"));
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("api.Resources.WindowIcon.png"));
        }
    }
}
