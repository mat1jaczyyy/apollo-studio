using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Interfaces;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Multi: Device, IMultipleChainParent, ISelectParent {
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

        public delegate void MultiResetHandler();
        public static event MultiResetHandler Reset;

        public static void InvokeReset() => Reset?.Invoke();

        public Chain Preprocess;
        public List<Chain> Chains = new List<Chain>();

        Random RNG = new Random();

        MultiType _mode;
        public MultiType Mode {
            get => _mode;
            set {
                _mode = value;

                if (SpecificViewer != null) ((MultiViewer)SpecificViewer).SetMode(Mode);
            }
        }

        int current = -1;
        ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();

        void Reroute() {
            Preprocess.Parent = this;
            Preprocess.MIDIExit = PreprocessExit;

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

        public override Device Clone() => new Multi(Preprocess.Clone(), (from i in Chains select i.Clone()).ToList(), Expanded, Mode) {
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

        void HandleReset() => current = -1;

        int? _expanded;
        public int? Expanded {
            get => _expanded;
            set {
                if (value != null && !(0 <= value && value < Chains.Count)) value = null;
                _expanded = value;                
            }
        }

        public Multi(Chain preprocess = null, List<Chain> init = null, int? expanded = null, MultiType mode = MultiType.Forward): base("multi") {
            Preprocess = preprocess?? new Chain();

            foreach (Chain chain in init?? new List<Chain>()) Chains.Add(chain);
            
            Expanded = expanded;

            Mode = mode;
            
            Reset += HandleReset;

            Reroute();
        }

        void ChainExit(Signal n) => MIDIExit?.Invoke(n);

        public override void MIDIProcess(Signal n) {
            Signal m = n.Clone();
            n.Color = new Color();

            if (!buffer.ContainsKey(n)) {
                if (!m.Color.Lit) return;

                if (Mode == MultiType.Forward) {
                    if (++current >= Chains.Count) current = 0;
                
                } else if (Mode == MultiType.Backward) {
                    if (--current < 0) current = Chains.Count - 1;
                
                } else if (Mode == MultiType.Random || current == -1)
                    current = RNG.Next(Chains.Count);
                
                else if (Mode == MultiType.RandomPlus) {
                    int old = current;
                    if (Chains.Count <= 1) current = 0;
                    else if ((current = RNG.Next(Chains.Count - 1)) >= old) current++;
                }

                m.MultiTarget.Push(buffer[n] = current);

            } else {
                m.MultiTarget.Push(buffer[n]);
                if (!m.Color.Lit) buffer.Remove(n, out int _);
            }

            Preprocess.MIDIEnter(m);
        }

        void PreprocessExit(Signal n) {
            if (n.GetType() == typeof(StopSignal)) return;

            int target = n.MultiTarget.Pop();
            
            if (Chains.Count == 0) MIDIExit?.Invoke(n);
            else Chains[target].MIDIEnter(n);
        }
        
        protected override void Stop() {
            foreach (Chain chain in Chains) chain.MIDIEnter(new StopSignal());
            Preprocess.MIDIEnter(new StopSignal());
        }

        public override void Dispose() {
            if (Disposed) return;

            Reset -= HandleReset;

            Preprocess.Dispose();
            foreach (Chain chain in Chains) chain.Dispose();
            base.Dispose();
        }
    }
}