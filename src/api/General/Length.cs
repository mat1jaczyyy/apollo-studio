using System;
using System.Collections.Generic;
using System.Linq;

using api;

namespace api {
    public class Length {
        private Decimal _value;
        
        public Decimal Value {
            get {
                return _value;
            }
            set {
                if ((Decimal)0.0078125 <= value && value <= (Decimal)4)
                    _value = value;
            }
        }

        public Length(int exponent = -2) {
            _value = (Decimal)0.25;
            Value = Convert.ToDecimal(Math.Pow(2, exponent));
        }

        public Length(Decimal value) {
            Value = value;
        }

        public static implicit operator Decimal(Length x) => x.Value * 240000 / Set.BPM;
    }
}