using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Structures;

namespace Apollo.Components {
    public class Indicator: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Display = this.Get<Ellipse>("Display");
        }

        Ellipse Display;
        Courier Timer;
        object locker = new object();
        
        bool Disposed = false;

        void SetIndicator(bool state) => Dispatcher.UIThread.InvokeAsync(() => Display.Opacity = Convert.ToInt32(state));

        public Indicator() => InitializeComponent();

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            lock (locker) {
                Timer?.Dispose();
                Disposed = true;
            }
        }

        public void Trigger() {
            if (!Preferences.DisplaySignalIndicators) return;

            lock (locker) {
                if (Disposed) return;

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
}
