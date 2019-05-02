using System;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public class Chain {
        public static readonly string Identifier = "chain";

        public ChainViewer Viewer;

        public IChainParent Parent = null;

        public delegate void ParentIndexChangedEventHandler(int index);
        public event ParentIndexChangedEventHandler ParentIndexChanged;
        public void ClearParentIndexChanged() => ParentIndexChanged = null;

        private int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                if (_ParentIndex != value) {
                    _ParentIndex = value;
                    ParentIndexChanged?.Invoke(_ParentIndex.Value);
                }
            }
        }

        private Action<Signal> _midiexit = null;
        public Action<Signal> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public List<Device> Devices = new List<Device>();
        private Action<Signal> _chainenter = null;

        private void Reroute() {
            for (int i = 0; i < Devices.Count; i++) {
                Devices[i].Parent = this;
                Devices[i].ParentIndex = i;
            }
            
            if (Devices.Count == 0)
                _chainenter = _midiexit;

            else {
                _chainenter = Devices[0].MIDIEnter;
                
                for (int i = 1; i < Devices.Count; i++)
                    Devices[i - 1].MIDIExit = Devices[i].MIDIEnter;
                
                Devices[Devices.Count - 1].MIDIExit = _midiexit;
            }
        }

        public Device this[int index] {
            get => Devices[index];
        }

        public int Count {
            get => Devices.Count;
        }

        public Chain Clone() => new Chain((from i in Devices select i.Clone()).ToList());

        public void Insert(int index, Device device) {
            Devices.Insert(index, device);
            Reroute();
        }

        public void Add(Device device) {
            Devices.Add(device);
            Reroute();
        }

        public void Remove(int index, bool dispose = true) {
            if (dispose) Devices[index].Dispose();
            Devices.RemoveAt(index);
            Reroute();
        }

        public Chain(List<Device> init = null) {
            Devices = init?? new List<Device>();
            Reroute();
        }

        public void MIDIEnter(Signal n) => _chainenter?.Invoke(n);

        public void Dispose() {
            foreach (Device device in Devices) device.Dispose();
            MIDIExit = null;
        }
    }
}