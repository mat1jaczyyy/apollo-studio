using System.Collections.Generic;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Rendering;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Preview: Device {
        public delegate void PreviewResetHandler();
        public static event PreviewResetHandler Clear;

        public static void InvokeClear() => Clear?.Invoke();

        Screen screen;

        void HandleClear() {
            screen.Clear();
            if (Viewer?.SpecificViewer != null) ((PreviewViewer)Viewer.SpecificViewer).Clear();
        }

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[0];

        public Preview(): base("preview") {
            screen = new Screen() { ScreenExit = PreviewExit };
            
            Clear += HandleClear;
        }
        
        public void PreviewExit(List<RawUpdate> n, Color[] snapshot) {
            if (Viewer?.SpecificViewer != null)
                n.ForEach(((PreviewViewer)Viewer.SpecificViewer).Render);
        }

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(screen.MIDIEnter);
            InvokeExit(n);
        }

        public override void Dispose() {
            if (Disposed) return;

            screen.Dispose();
            Clear -= HandleClear;

            base.Dispose();
        }
    }
}