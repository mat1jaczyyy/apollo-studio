using System;

using Apollo.Structures;

namespace Apollo.Rendering {
    abstract class Renderer {
        public static Renderer Current;

        static Renderer() {
            // TODO TEMPORARY
            Current = new Heaven();
        }

        public abstract void MIDIEnter(Signal n);
    }
}