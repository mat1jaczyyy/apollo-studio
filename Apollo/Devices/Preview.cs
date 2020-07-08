using System.Collections.Generic;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Rendering;
using Apollo.Structures;

namespace Apollo.Devices {
    //! Heaven incompatible
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

        public override IEnumerable<Signal> MIDIProcess(IEnumerable<Signal> n) {
            foreach (Signal i in n)
                screen.MIDIEnter(i.Clone());  // TODO this shit broke, idk what do with it

            return n;
        }

        public override void Dispose() {
            if (Disposed) return;

            Clear -= HandleClear;

            base.Dispose();
        }
    }
}