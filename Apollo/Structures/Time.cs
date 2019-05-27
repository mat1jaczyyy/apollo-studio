using System;

namespace Apollo.Structures {
    public class Time {
        public delegate void ValueChangedEventHandler(int free);
        public event ValueChangedEventHandler FreeChanged;
        public event ValueChangedEventHandler StepChanged;

        public delegate void ModeChangedEventHandler(bool mode);
        public event ModeChangedEventHandler ModeChanged;

        private int _min = 0;
        public int Minimum {
            get => _min;
            set {
                _min = value;

                if (_free < _min)
                    Free = _min;
            }
        }
        
        private int _max = int.MaxValue;
        public int Maximum {
            get => _max;
            set {
                _max = value;
                
                if (_max < _free)
                    Free = _max;
            }
        }

        private int _free;
        public int Free {
            get => _free;
            set {
                if (Minimum <= value && value <= Maximum && _free != value) {
                    _free = value;
                    FreeChanged?.Invoke(_free);
                }
            }
        }

        private bool _mode; // true uses Length
        public bool Mode {
            get => _mode;
            set {
                if (_mode != value) {
                    _mode = value;
                    ModeChanged?.Invoke(_mode);
                }
            }
        }

        public Length Length;
        private void LengthChanged() => StepChanged?.Invoke(Length.Step);

        public Time Clone() => new Time(_mode, Length.Clone(), _free) {
            Minimum = Minimum,
            Maximum = Maximum
        };

        public Time(bool mode = false, Length length = null, int free = 1000) {
            _free = free;
            _mode = mode;
            Length = length?? new Length();

            Length.Changed += LengthChanged;
        }

        public override bool Equals(object obj) {
            if (!(obj is Time)) return false;
            return this == (Time)obj;
        }

        public static bool operator ==(Time a, Time b) {
            if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, b);
            return a.Mode == b.Mode && a.Length == b.Length && a.Free == b.Free;
        }
        public static bool operator !=(Time a, Time b) => !(a == b);

        public override int GetHashCode() => HashCode.Combine(Mode, Length, Free);

        public static implicit operator int(Time x) => x.Mode? (int)x.Length : x.Free;
        public static implicit operator double(Time x) => x.Mode? (double)x.Length : x.Free;
        public static implicit operator decimal(Time x) => x.Mode? x.Length : x.Free;
        
        public override string ToString() => Mode? Length.ToString() : $"{Free}ms";
    }
}