using System.Collections.Concurrent;
using System.IO;
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
        object locker = new object();
        ConcurrentDictionary<(Launchpad, int, int), Signal> signals = new ConcurrentDictionary<(Launchpad, int, int), Signal>();

        void HandleChoke(Choke sender, int index) {
            if (Target == index && sender != this && !choked) {
                choked = true;
                Chain.MIDIEnter(new StopSignal());
                
                lock (locker) {
                    foreach (Signal i in signals.Values) {
                        i.Color = new Color(0);
                        InvokeExit(i);
                    }

                    signals = new ConcurrentDictionary<(Launchpad, int, int), Signal>();
                }
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

        void ChainExit(Signal n) {
            if (!choked) {
                InvokeExit(n.Clone());
                
                lock (locker) {
                    (Launchpad, int, int) index = (n.Source, n.Index, -n.Layer);
                    if (n.Color.Lit) signals[index] = n.Clone();
                    else if (signals.ContainsKey(index)) signals.TryRemove(index, out _);
                }
            }
        }

        public override void MIDIProcess(Signal n) {
            if (choked && n.Color.Lit) {
                Choked?.Invoke(this, Target);
                choked = false;
            }

            if (!choked) Chain.MIDIEnter(n.Clone());
        }
        
        protected override void Stop() {
            Chain.MIDIEnter(new StopSignal());
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
            
            TargetUndoEntry(BinaryReader reader, int version): base(reader, version){}
        }
    }
}