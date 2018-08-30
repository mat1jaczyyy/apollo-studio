using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public class Group: Device {
        private List<Chain> _chains = new List<Chain>();

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
            return new Group((from chain in _chains select chain.Clone()).ToList());
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

        public Group() {}

        public Group(Chain[] init) {
            Add(init);
        }

        public Group(List<Chain> init) {
            Add(init.ToArray());
        }

        public Group(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Group(Chain[] init, Action<Signal> exit) {
            Add(init);
            MIDIExit = exit;
        }

        public Group(List<Chain> init, Action<Signal> exit) {
            Add(init.ToArray());
            MIDIExit = exit;
        }

        private void ChainExit(Signal n) {
            if (MIDIExit != null)
                MIDIExit(n);
        }

        public override void MIDIEnter(Signal n) {
            foreach (Chain chain in _chains)
                chain.MIDIEnter(n.Clone());
        }
    }
}