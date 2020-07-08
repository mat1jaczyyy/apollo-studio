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

        public bool ChainKind { get; set; } = false;

        Ellipse Display;
        Courier Timer;
        object locker = new object();
        
        bool Disposed = false;

        void SetIndicator(double state) => Dispatcher.UIThread.InvokeAsync(() => Display.Opacity = state);

        public Indicator() {
            InitializeComponent();
            
            Timer = new Courier(200, _ => SetIndicator(0), false);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            lock (locker) {
                Timer?.Dispose();
                Disposed = true;
            }
        }

        public void Trigger(bool lit = true) { //(bool lit) { TODO Heaven rewrite?
            if (ChainKind? !Preferences.ChainSignalIndicators : !Preferences.DeviceSignalIndicators) return;

            lock (locker) {
                if (Disposed) return;

                SetIndicator(lit? 1 : 0.5);

                Timer.Restart();
            }
        }
    }
}
