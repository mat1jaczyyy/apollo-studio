using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
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

        public override Device Clone() {
            Group ret = new Group();
            foreach (Chain chain in _chains)
                ret.Add(chain.Clone());
            return ret;
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

        public Group(List<Chain> init) {
            _chains = init;
            foreach (Chain chain in _chains)
                chain.MIDIExit = ChainExit;
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

        public Group(List<Chain> init, Action<Signal> exit) {
            _chains = init;
            foreach (Chain chain in _chains)
                chain.MIDIExit = ChainExit;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            foreach (Chain chain in _chains)
                chain.MIDIEnter(n);
        }
    }
}