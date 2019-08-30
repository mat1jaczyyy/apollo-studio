using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Structures;

namespace Apollo.Components {
    public class Indicator: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Display = this.Get<Ellipse>("Display");
        }
        
        Ellipse Display;
        Courier Timer;

        void SetIndicator(bool state) => Dispatcher.UIThread.InvokeAsync(() => Display.Opacity = Convert.ToInt32(state));

        public Indicator() => InitializeComponent();

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => Timer?.Dispose();

        public void Trigger() {
            Timer?.Dispose();

            Timer = new Courier() {
                Interval = 200
            };

            Timer.Elapsed += (_, __) => SetIndicator(false);
            Timer.Start();

            SetIndicator(true);
        }
    }
}
