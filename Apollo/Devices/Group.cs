using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Group: Device, IChainParent {
        public static readonly new string DeviceIdentifier = "group";

        private Action<Signal> _midiexit;
        public override Action<Signal> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        private List<Chain> _chains = new List<Chain>();

        private void Reroute() {
            for (int i = 0; i < _chains.Count; i++) {
                _chains[i].Parent = this;
                _chains[i].ParentIndex = i;
                _chains[i].MIDIExit = ChainExit;
            }
        }

        public Chain this[int index] {
            get => _chains[index];
        }

        public int Count {
            get => _chains.Count;
        }

        public override Device Clone() => new Group((from i in _chains select i.Clone()).ToList(), Expanded);

        public void Insert(int index, Chain chain = null) {
            _chains.Insert(index, chain?? new Chain());
            
            Reroute();
        }

        public void Add(Chain chain) {
            _chains.Add(chain);

            Reroute();
        }

        public void Remove(int index) {
            _chains[index].Dispose();
            _chains.RemoveAt(index);

            Reroute();
        }

        public int? Expanded;

        public Group(List<Chain> init = null, int? expanded = null): base(DeviceIdentifier) {
            foreach (Chain chain in init?? new List<Chain>()) _chains.Add(chain);
            Expanded = expanded;

            Reroute();
        }

        private void ChainExit(Signal n) => MIDIExit?.Invoke(n);

        public override void MIDIEnter(Signal n) {
            if (_chains.Count == 0) ChainExit(n);

            foreach (Chain chain in _chains)
                chain.MIDIEnter(n.Clone());
        }

        public override void Dispose() {
            foreach (Chain chain in _chains) chain.Dispose();
            base.Dispose();
        }
    }
}