using System;
using System.Collections.Generic;

using Apollo.Interfaces;

namespace Apollo.Helpers {
    public class Copyable {
        public List<ISelect> Contents = new List<ISelect>();

        public Type Type => (Contents.Count > 0)? Contents[0].GetType() : null;
    }
}