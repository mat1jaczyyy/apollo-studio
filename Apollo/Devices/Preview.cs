using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    //? might work after screen gets implemented
    public class Preview: Device {
        public delegate void PreviewResetHandler();
        public static event PreviewResetHandler Clear;

        public static void InvokeClear() => Clear?.Invoke();

        Screen screen;

        void HandleClear() {
            screen = new Screen() { ScreenExit = PreviewExit };
            if (Viewer?.SpecificViewer != null) ((PreviewViewer)Viewer.SpecificViewer).Clear();
        }

        public override Device Clone() => new Preview() {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Preview(): base("preview") {
            screen = new Screen() { ScreenExit = PreviewExit };
            
            Clear += HandleClear;
        }
        
        public void PreviewExit(Signal n) {
            if (Viewer?.SpecificViewer != null) ((PreviewViewer)Viewer.SpecificViewer).Signal(n);
        }

        public override void MIDIProcess(Signal n) {
            Signal m = n.Clone();
            InvokeExit(n);
            screen.MIDIEnter(m);
        }

        public override void Dispose() {
            if (Disposed) return;

            Clear -= HandleClear;

            base.Dispose();
        }
    }
}