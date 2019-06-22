namespace Apollo.Structures {
    public class Offset {
        public delegate void ChangedEventHandler(Offset sender);
        public event ChangedEventHandler Changed;

        private int _x = 0;
        public int X {
            get => _x;
            set {
                if (-9 <= value && value <= 9 && _x != value) {
                    _x = value;
                    Changed?.Invoke(this);
                }
            }
        }

        private int _y = 0;
        public int Y {
            get => _y;
            set {
                if (-9 <= value && value <= 9 && _y != value) {
                    _y = value;
                    Changed?.Invoke(this);
                }
            }
        }

        public Offset Clone() => new Offset(X, Y);

        public Offset(int x = 0, int y = 0) {
            X = x;
            Y = y;
        }

        public void Dispose() => Changed = null;
    }
}