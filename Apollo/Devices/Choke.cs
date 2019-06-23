using System;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Choke: Device, IChainParent {
        public static readonly new string DeviceIdentifier = "choke";
        
        private int _target = 1;
        public int Target {
            get => _target;
            set {
                if (1 <= value && value <= 16) {
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

        public override Device Clone() => new Choke(Target, Chain.Clone()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Choke(int target = 1, Chain chain = null): base(DeviceIdentifier) {
            Target = target;
            Chain = chain?? new Chain();
        }

        private void ChainExit(Signal n) => MIDIExit?.Invoke(n);

        public override void MIDIProcess(Signal n) {
            Chain.MIDIEnter(n.Clone());
        }

        public override void Dispose() {
            Chain.Dispose();
            base.Dispose();
        }
    }
}