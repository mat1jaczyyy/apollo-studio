using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Preview: Device {
        private Screen screen;

        public override Device Clone() => new Preview() {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Preview(): base("preview") => screen = new Screen() { ScreenExit = PreviewExit };

        public delegate void SignalExitedEventHandler(Signal n);
        public event SignalExitedEventHandler SignalExited;
        
        public void PreviewExit(Signal n) => SignalExited?.Invoke(n);

        public override void MIDIProcess(Signal n) {
            Signal m = n.Clone();
            MIDIExit?.Invoke(n);
            screen.MIDIEnter(m);
        }

        public override void Dispose() {
            SignalExited = null;
            base.Dispose();
        }
    }
}