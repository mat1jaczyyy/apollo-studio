using System;

using Apollo.Core;

namespace Apollo.Structures {
    public class Length {
        public delegate void ChangedEventHandler();
        public event ChangedEventHandler Changed;

        private static string[] Steps = new string[]
            {"1/128", "1/64", "1/32", "1/16", "1/8", "1/4", "1/2", "1/1", "2/1", "4/1"};

        private int _value;
        public int Step {
            get => _value;
            set {
                if (0 <= value && value <= 9 && _value != value) {
                    _value = value;
                    Changed?.Invoke();
                }
            }
        }
        
        public decimal Value {
            get => Convert.ToDecimal(Math.Pow(2, _value - 7));
        }

        public Length Clone() => new Length(Step);

        public Length(int step = 5) => Step = step;

        public static implicit operator decimal(Length x) => x.Value * 240000 / Program.Project.BPM;

        public override string ToString() => Steps[_value];
    }
}