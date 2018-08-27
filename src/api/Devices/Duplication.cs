using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Duplication: Device {
        private List<int> _offsets;

        public List<int> Offsets {
            get {
                return _offsets;
            }
            set {
                foreach (int offset in value) {
                    if (offset <= -127 || 127 <= offset) return;
                }
                _offsets = value;
            }
        }

        public override Device Clone() {
            return new Duplication(_offsets);
        }

        public void Insert(int index, int offset) {
            if (offset <= -127 || 127 <= offset)
                _offsets.Insert(index, offset);
        }

        public void Add(int offset) {
            if (offset <= -127 || 127 <= offset)
                _offsets.Add(offset);
        }

        public void Remove(int index) {
            _offsets.RemoveAt(index);
        }

        public Duplication() {
            _offsets = new List<int>();
            MIDIExit = null;
        }

        public Duplication(int[] offsets) {
            Offsets = offsets.ToList();
            MIDIExit = null;
        }

        public Duplication(List<int> offsets) {
            Offsets = offsets;
            MIDIExit = null;
        }

        public Duplication(Action<Signal> exit) {
            _offsets = new List<int>();
            MIDIExit = exit;
        }

        public Duplication(int[] offsets, Action<Signal> exit) {
            Offsets = offsets.ToList();
            MIDIExit = exit;
        }

        public Duplication(List<int> offsets, Action<Signal> exit) {
            Offsets = offsets;
            MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (MIDIExit != null)
                MIDIExit(n);
            
            foreach (int offset in _offsets) {
                Signal m = n.Clone();

                int result = m.Index + offset;
                
                if (result < 0) result = 0;
                if (result > 127) result = 127;

                m.Index = (byte)result;

                if (MIDIExit != null)
                    MIDIExit(m);
            }
        }
    }
}