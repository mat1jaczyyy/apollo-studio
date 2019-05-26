namespace Apollo.Structures {
    public class Time {
        public delegate void ValueChangedEventHandler(int free);
        public event ValueChangedEventHandler FreeChanged;
        public event ValueChangedEventHandler StepChanged;

        public delegate void ModeChangedEventHandler(bool mode);
        public event ModeChangedEventHandler ModeChanged;

        public int Minimum = 0, Maximum = int.MaxValue;

        private int _free;
        public int Free {
            get => _free;
            set {
                if (Minimum <= value && value <= Maximum && _free != value)
                    FreeChanged?.Invoke(_free = value);
            }
        }

        private bool _mode; // true uses Length
        public bool Mode {
            get => _mode;
            set {
                if (_mode != value)
                    ModeChanged?.Invoke(_mode = value);
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

        public static implicit operator int(Time x) => x.Mode? (int)x.Length : x.Free;
        public static implicit operator double(Time x) => x.Mode? (double)x.Length : x.Free;
        public static implicit operator decimal(Time x) => x.Mode? x.Length : x.Free;
    }
}