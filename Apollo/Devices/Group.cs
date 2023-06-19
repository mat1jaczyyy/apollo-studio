using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia.Controls;

using Apollo.Binary;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Group: Device, IChainParent, ISelectParent {
        public GroupViewer SpecificViewer => (GroupViewer)Viewer?.SpecificViewer;

        public ISelectParentViewer IViewer => (ISelectParentViewer)Viewer?.SpecificViewer;

        public List<ISelect> IChildren => Chains.Select(i => (ISelect)i).ToList();

        public bool IRoot => false;

        public void IInsert(int index, ISelect item) => Insert(index, (Chain)item);
        
        public Window IWindow => Track.Get(this)?.Window;
        public SelectionManager Selection => Track.Get(this)?.Window?.Selection;

        public Type ChildType => typeof(Chain);
        public string ChildString => "Chain";
        public string ChildFileExtension => "apchn";

        Action<List<Signal>> _midiexit;
        public override Action<List<Signal>> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public List<Chain> Chains = new();
        protected virtual void Reroute() {
            for (int i = 0; i < Chains.Count; i++) {
                Chains[i].Parent = this;
                Chains[i].ParentIndex = i;
                Chains[i].MIDIExit = InvokeExit;
            }
        }

        public Chain this[int index] => Chains[index];
        public int Count => Chains.Count;

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

        protected int? _expanded;
        public int? Expanded {
            get => _expanded;
            set {
                if (value != null && !(0 <= value && value < Chains.Count)) value = null;
                _expanded = value;                
            }
        }

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Chains.Select(i => i.Clone(purpose)).ToList(), Expanded, Type.Missing };

        public Group(List<Chain> init = null, int? expanded = null, string identifier = "group"): base(identifier) {
            foreach (Chain chain in init?? new List<Chain>()) Chains.Add(chain);
            Expanded = expanded;

            Reroute();
        }

        public override void MIDIProcess(List<Signal> n) {
            if (Chains.Count == 0) InvokeExit(n);

            foreach (Chain chain in Chains)
                chain.MIDIEnter(n.Select(i => i.Clone()).ToList());
        }
        
        protected override void Stopped() {
            foreach (Chain chain in Chains)
                chain.MIDIEnter(StopSignal.Instance);
        }

        public override void Dispose() {
            if (Disposed) return;

            foreach (Chain chain in Chains) chain.Dispose();
            base.Dispose();
        }
                
        public class ChainInsertedUndoEntry: PathUndoEntry<Group> {
            int index;
            Chain chain;

            protected override void UndoPath(params Group[] items) => items[0].Remove(index);
            protected override void RedoPath(params Group[] items) => items[0].Insert(index, chain.Clone(PurposeType.Active));
            
            protected override void OnDispose() => chain.Dispose();
            
            public ChainInsertedUndoEntry(Group group, int index, Chain chain)
            : base($"{group.Name} Chain {index + 1} Inserted", group) {
                this.index = index;
                this.chain = chain.Clone(PurposeType.Passive);
            }
            
            ChainInsertedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                index = reader.ReadInt32();
                chain = Decoder.Decode<Chain>(reader, version, PurposeType.Passive);
            }
            
            public override void Encode(BinaryWriter writer) { 
                base.Encode(writer);
                
                writer.Write(index);
                Encoder.Encode(writer, chain);
            }
        }
    }
}