using System;
using System.Collections.Generic;
using System.Linq;

using api;

namespace api.Devices {
    public abstract class Device {
        public abstract void MIDIEnter(Signal n);
        public Action<Signal> MIDIExit;
    }

    public class Group: Device {
        private List<Chain> _chains;

        private void ChainExit(Signal n) {
            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }

        public Chain this[int index] {
            get {
                return _chains[index];
            }
            set {
                _chains[index] = value;
                _chains[index].MIDIExit = ChainExit;
            }
        }

        public int Count {
            get {
                return _chains.Count;
            }
        }

        public void Insert(int index) {
            _chains.Insert(index, new Chain(ChainExit));
        }

        public void Insert(int index, Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Insert(index, chain);
        }

        public void Add() {
            _chains.Add(new Chain(ChainExit));
        }

        public void Add(Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Add(chain);  
        }

        public void Add(Chain[] chains) {
            foreach (Chain chain in chains) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }     
        }

        public void Remove(int index) {
            _chains.RemoveAt(index);
        }

        public Group() {
            _chains = new List<Chain>();
            this.MIDIExit = null;
        }

        public Group(Chain[] init) {
            _chains = new List<Chain>();
            foreach (Chain chain in init) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }
            this.MIDIExit = null;
        }

        public Group(Action<Signal> exit) {
            _chains = new List<Chain>();
            this.MIDIExit = exit;
        }

        public Group(Chain[] init, Action<Signal> exit) {
            _chains = new List<Chain>();
            foreach (Chain chain in init) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            foreach (Chain chain in _chains)
                chain.MIDIEnter(n);
        }
    }

    public class Pitch: Device {
        // Note offset
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= _offset && _offset <= 128) _offset = value;
            }
        }

        public Pitch() {
            this._offset = 0;
            this.MIDIExit = null;
        }

        public Pitch(int offset) {
            this.Offset = offset;
            this.MIDIExit = null;
        }

        public Pitch(Action<Signal> exit) {
            this._offset = 0;
            this.MIDIExit = exit;
        }

        public Pitch(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            int result = (int)(n.p) + _offset;

            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }

    public class Chord: Device {
        // Note offset
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= _offset && _offset <= 128) _offset = value;
            }
        }

        public Chord() {
            this._offset = 0;
            this.MIDIExit = null;
        }

        public Chord(int offset) {
            this.Offset = offset;
            this.MIDIExit = null;
        }

        public Chord(Action<Signal> exit) {
            this._offset = 0;
            this.MIDIExit = exit;
        }

        public Chord(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (this.MIDIExit != null)
                this.MIDIExit(n);
            
            int result = (int)(n.p) + _offset;
            
            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }
}