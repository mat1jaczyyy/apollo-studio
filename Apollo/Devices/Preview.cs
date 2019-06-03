using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Preview: Device {
        public static readonly new string DeviceIdentifier = "preview";

        private Pixel[] screen = new Pixel[100];

        public override Device Clone() => new Preview() {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Preview(): base(DeviceIdentifier) {
            for (int i = 0; i < 100; i++)
                screen[i] = new Pixel() {MIDIExit = PreviewExit};
        }

        public delegate void SignalExitedEventHandler(Signal n);
        public event SignalExitedEventHandler SignalExited;
        
        public void PreviewExit(Signal n) => SignalExited?.Invoke(n);

        public override void MIDIProcess(Signal n) {
            Signal m = n.Clone();
            MIDIExit?.Invoke(n);
            screen[m.Index].MIDIEnter(m);
        }
    }
}