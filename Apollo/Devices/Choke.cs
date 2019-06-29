using System;
using System.Collections.Concurrent;
using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Interfaces;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Choke: Device, IChainParent {
        public delegate void ChokedEventHandler(Choke sender, int index);
        public static event ChokedEventHandler Choked;
        
        private int _target = 1;
        public int Target {
            get => _target;
            set {
                if (_target != value && 1 <= value && value <= 16) {
                   _target = value;

                   if (Viewer?.SpecificViewer != null) ((ChokeViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        private Chain _chain;
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
        ConcurrentDictionary<(int, int), Signal> signals = new ConcurrentDictionary<(int, int), Signal>();

        private void HandleChoke(Choke sender, int index) {
            if (Target == index && sender != this && !choked) {
                choked = true;
                Chain.MIDIEnter(new StopSignal());
                
                lock (locker) {
                    foreach (Signal i in signals.Values) {
                        i.Color = new Color(0);
                        MIDIExit?.Invoke(i);
                    }

                    signals = new ConcurrentDictionary<(int, int), Signal>();
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

        private void ChainExit(Signal n) {
            if (!choked) {
                MIDIExit?.Invoke(n.Clone());
                
                lock (locker) {
                    (int, int) index = (n.Index, -n.Layer);
                    if (n.Color.Lit) signals[index] = n.Clone();
                    else if (signals.ContainsKey(index)) signals.TryRemove(index, out Signal _);
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

        public override void Dispose() {
            Choked -= HandleChoke;

            Chain.Dispose();
            base.Dispose();
        }
    }
}