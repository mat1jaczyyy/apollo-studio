using System;

using Apollo.Core;

namespace Apollo.Structures {
    public class Length {
        private Decimal _value = 0.25M;
        
        public Decimal Value {
            get => _value;
            set {
                if ((Decimal)0.0078125 <= value && value <= (Decimal)4)
                    _value = value;
            }
        }

        public Length(int exponent = -2) => Value = Convert.ToDecimal(Math.Pow(2, exponent));

        public Length(Decimal value) => Value = value;

        public static implicit operator Decimal(Length x) => x.Value * 240000 / Program.Project.BPM;
    }
}