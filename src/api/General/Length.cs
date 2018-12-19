using System;
using System.Collections.Generic;
using System.Linq;

using api;

namespace api {
    public class Length {
        public Decimal Value = 1 / 4;

        public Length(int exponent) {
            Value = Convert.ToDecimal(Math.Pow(2, exponent));
        }

        public Length(Decimal value) {
            Value = value;
        }

        public static implicit operator Decimal(Length x) => x.Value * 240000 / Set.BPM;
    }
}