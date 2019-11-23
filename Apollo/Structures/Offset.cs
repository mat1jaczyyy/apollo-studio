using System;

namespace Apollo.Structures {
    public class Offset {
        public delegate void ChangedEventHandler(Offset sender);
        public event ChangedEventHandler Changed;
        
        public delegate void AngleChangedEventHandler(Offset sender);
        public event AngleChangedEventHandler AngleChanged;

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
        
        double _angle = 0;
        public double Angle {
            get => _angle;
            set {
                if(-Math.PI <= value && value <= Math.PI && value != _angle){
                    _angle = value;
                    AngleChanged?.Invoke(this);
                }
            }
        }
        
        public Offset Clone() => new Offset(X, Y, Angle);

        public Offset(int x = 0, int y = 0, double angle = 0) {
            X = x;
            Y = y;
            Angle = angle;
        }
        
        public DoubleTuple ToTuple() => new DoubleTuple((double)_x, (double)_y);

        public void Dispose() => Changed = null;
    }
}