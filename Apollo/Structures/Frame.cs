using System.Linq;

namespace Apollo.Structures {
    public class Frame {
        public Color[] Screen;

        public bool Mode; // true uses Length
        public Length Length;
        private int _time;

        public int Time {
            get => _time;
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
            }
        }

        public string TimeString => Mode? Length.ToString() : $"{Time}ms";

        public Frame Clone() => new Frame(Mode, Length.Clone(), Time, (from i in Screen select i.Clone()).ToArray());

        public Frame(bool mode = false, Length length = null, int time = 1000, Color[] screen = null) {
            if (screen == null || screen.Length != 100) {
                screen = new Color[100];
                for (int i = 0; i < 100; i++) screen[i] = new Color(0);
            }

            Mode = mode;
            Time = time;
            Length = length?? new Length();
            Screen = screen;
        }
    }
}