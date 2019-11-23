using System;

namespace Apollo.Structures{
    public struct DoubleTuple {
        public double X;
        public double Y;
        
        public DoubleTuple(double x = 0.0, double y = 0.0){
            X = x;
            Y = y;
        }
        
        public IntTuple Round(){
            return new IntTuple((int)Math.Round(X), (int)Math.Round(Y));
        }
        
        public static DoubleTuple operator * (DoubleTuple t, double f) => new DoubleTuple(t.X * f, t.Y * f);
        public static DoubleTuple operator + (DoubleTuple a, DoubleTuple b) => new DoubleTuple(a.X + b.X, a.Y + b.Y);
    };
    
    public struct IntTuple {
        public int X;
        public int Y;
        
        public IntTuple(int x, int y){
            X = x;
            Y = y;
        }
    }
}