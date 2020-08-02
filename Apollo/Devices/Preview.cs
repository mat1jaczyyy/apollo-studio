using System.Collections.Generic;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Rendering;
using Apollo.Structures;

namespace Apollo.Devices {
    //? might work after screen gets implemented
    public class Preview: Device {
        public delegate void PreviewResetHandler();
        public static event PreviewResetHandler Clear;

        public static void InvokeClear() => Clear?.Invoke();

        Screen screen;

        void HandleClear() {
            screen = new Screen() { /*ScreenExit = PreviewExit*/ };
            if (Viewer?.SpecificViewer != null) ((PreviewViewer)Viewer.SpecificViewer).Clear();
        }

        public override Device Clone() => new Preview() {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Preview(): base("preview") {
            screen = new Screen() { /*ScreenExit = PreviewExit*/ }; // TODO heaven dispose
            
            Clear += HandleClear;
        }
        
        public void PreviewExit(Screen sender, List<Signal> n) {
            if (Viewer?.SpecificViewer != null) {
                foreach (Signal i in n)
                    ((PreviewViewer)Viewer.SpecificViewer).Signal(i);
            }
        }

        public override void MIDIProcess(IEnumerable<Signal> n) {
            //Signal m = n.Clone();
            InvokeExit(n);
            //screen.MIDIEnter(m);
        }

        public override void Dispose() {
            if (Disposed) return;

            Clear -= HandleClear;

            base.Dispose();
        }
    }
}