using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Choke: Device, IChainParent {
        public delegate void ChokedEventHandler(Choke sender, int index);
        public static event ChokedEventHandler Choked;
        
        int _target = 1;
        public int Target {
            get => _target;
            set {
                if (_target != value && 1 <= value && value <= 16) {
                   _target = value;

                   if (Viewer?.SpecificViewer != null) ((ChokeViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        Chain _chain;
        public Chain Chain {
            get => _chain;
            set {
                if (_chain != null) {
                    Chain.Parent = null;
                    Chain.ParentIndex = null;
                    Chain.MIDIExit = null;
                }

                _chain = value;

                if (_chain != null) {
                    Chain.Parent = this;
                    Chain.ParentIndex = 0;
                    Chain.MIDIExit = ChainExit;
                }
            }
        }

        bool choked = true;
        ConcurrentDictionary<(Launchpad, int, int), Signal> signals = new();

        void HandleChoke(Choke sender, int index) {
            if (Target == index && sender != this && !choked) {
                choked = true;
                Chain.MIDIEnter(StopSignal.Instance);
                
                List<Signal> o = signals.Values.ToList();
                o.ForEach(i => i.Color = new Color(0));
                InvokeExit(o);

                signals.Clear();
            }
        }

        public override Device Clone() => new Choke(Target, Chain.Clone()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Choke(int target = 1, Chain chain = null): base("choke") {
            Target = target;
            Chain = chain?? new Chain();

            Choked += HandleChoke;
        }

        void ChainExit(List<Signal> n) {
            if (choked) return;

            InvokeExit(n);
            
            n.ForEach(i => {
                (Launchpad, int, int) index = (i.Source, i.Index, -i.Layer);
                if (i.Color.Lit) signals[index] = i.Clone();
                else if (signals.ContainsKey(index)) signals.TryRemove(index, out _);
            });
        }

        public override void MIDIProcess(List<Signal> n) {
            IEnumerable<Signal> o = n;

            if (choked) {
                IEnumerable<Signal> m = n.SkipWhile(i => !i.Color.Lit);

                if (m.Any()) {
                    Choked?.Invoke(this, Target);
                    choked = false;

                    o = m;
                }
            }

            if (!choked) Chain.MIDIEnter(o.Select(i => i.Clone()).ToList());
        }
        
        protected override void Stopped() {
            Chain.MIDIEnter(StopSignal.Instance);

            signals.Clear();
            choked = true;
        }

        public override void Dispose() {
            if (Disposed) return;

            Choked -= HandleChoke;

            Chain.Dispose();
            base.Dispose();
        }
        
        void SetTarget(int target) => Target = target;
        
        public class TargetUndoEntry: SimplePathUndoEntry<Choke, int> {
            protected override void Action(Choke item, int element) => item.SetTarget(element);

            public TargetUndoEntry(Choke choke, int u, int r)
            : base($"Choke Target Changed to {r}", choke, u, r) {}
            
            TargetUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}