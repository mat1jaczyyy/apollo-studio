using Apollo.Enums;

namespace Apollo.Structures {
    public class Offset {
        public delegate void ChangedEventHandler(Offset sender);
        public event ChangedEventHandler Changed;

        int _x = 0;
        public int X {
            get => _x;
            set {
                if (-9 <= value && value <= 9 && _x != value) {
                    _x = value;
                    Changed?.Invoke(this);
                }
            }
        }

        int _y = 0;
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
        
        static int Wrap(int coord, GridType gridMode) => (gridMode == GridType.Square)? ((coord + 7) % 8 + 1) : (coord + 10) % 10;

        public static bool Validate(int x, int y, GridType gridMode, bool wrap, out int result) {
            if (wrap) {
                x = Wrap(x, gridMode);
                y = Wrap(y, gridMode);
            }

            result = y * 10 + x;

            if (gridMode == GridType.Full) {
                if (0 <= x && x <= 9 && 0 <= y && y <= 9)
                    return true;
                
                if (y == -1 && 4 <= x && x <= 5) {
                    result = 100;
                    return true;
                }

            } else if (gridMode == GridType.Square)
                if (1 <= x && x <= 8 && 1 <= y && y <= 8)
                    return true;
             
            return false;
        }

        public bool Apply(int index, GridType gridMode, bool wrap, out int x, out int y, out int result) {
            x = index % 10;
            y = index / 10;

            if (gridMode == GridType.Square && (x == 0 || x == 9 || y == 0 || y == 9)) {
                result = 0;
                return false;
            }

            x += X;
            y += Y;

            return Validate(x, y, gridMode, wrap, out result);
        }

        public void Dispose() => Changed = null;
    }
}