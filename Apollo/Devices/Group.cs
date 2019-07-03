using System;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Interfaces;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Group: Device, IMultipleChainParent, ISelectParent {
        public IMultipleChainParentViewer SpecificViewer {
            get => (IMultipleChainParentViewer)Viewer?.SpecificViewer;
        }

        public ISelectParentViewer IViewer {
            get => (ISelectParentViewer)Viewer?.SpecificViewer;
        }

        public List<ISelect> IChildren {
            get => Chains.Select(i => (ISelect)i).ToList();
        }

        public bool IRoot {
            get => false;
        }

        Action<Signal> _midiexit;
        public override Action<Signal> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public List<Chain> Chains = new List<Chain>();

        void Reroute() {
            for (int i = 0; i < Chains.Count; i++) {
                Chains[i].Parent = this;
                Chains[i].ParentIndex = i;
                Chains[i].MIDIExit = ChainExit;
            }
        }

        public Chain this[int index] {
            get => Chains[index];
        }

        public int Count {
            get => Chains.Count;
        }

        public override Device Clone() => new Group((from i in Chains select i.Clone()).ToList(), Expanded) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public void Insert(int index, Chain chain = null) {
            Chains.Insert(index, chain?? new Chain());
            Reroute();

            SpecificViewer?.Contents_Insert(index, Chains[index]);
            
            Track.Get(this)?.Window?.Selection.Select(Chains[index]);
            SpecificViewer?.Expand(index);
        }

        public void Remove(int index, bool dispose = true) {
            if (index < Chains.Count - 1)
                Track.Get(this)?.Window?.Selection.Select(Chains[index + 1]);
            else if (Chains.Count > 1)
                Track.Get(this)?.Window?.Selection.Select(Chains[Chains.Count - 2]);
            else
                Track.Get(this)?.Window?.Selection.Select(null);
                
            SpecificViewer?.Contents_Remove(index);

            if (dispose) Chains[index].Dispose();
            Chains.RemoveAt(index);
            Reroute();
        }

        public int? Expanded { get; set; }

        public Group(List<Chain> init = null, int? expanded = null): base("group") {
            foreach (Chain chain in init?? new List<Chain>()) Chains.Add(chain);
            Expanded = expanded;

            Reroute();
        }

        void ChainExit(Signal n) => MIDIExit?.Invoke(n);

        public override void MIDIProcess(Signal n) {
            if (Chains.Count == 0) ChainExit(n);

            foreach (Chain chain in Chains)
                chain.MIDIEnter(n.Clone());
        }

        protected override void Stop() {
            foreach (Chain chain in Chains) chain.MIDIEnter(new StopSignal());
        }

        public override void Dispose() {
            foreach (Chain chain in Chains) chain.Dispose();
            base.Dispose();
        }
    }
}