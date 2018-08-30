using System;
using System.Collections.Generic;
using System.Linq;
using api.Devices;

namespace api {
    public class Chain {
        private List<Device> _devices = new List<Device>();
        private Action<Signal> _chainenter = null;
        private Action<Signal> _midiexit = null;
        public Range Zone = new Range();

        private void Reroute() {
            if (_devices.Count == 0) {
                _chainenter = _midiexit;
            } else {
                _chainenter = _devices[0].MIDIEnter;
                for (int i = 1; i < _devices.Count; i++)
                    _devices[i - 1].MIDIExit = _devices[i].MIDIEnter;
                _devices[_devices.Count - 1].MIDIExit = _midiexit;
            }
        }

        public Device this[int index] {
            get {
                return _devices[index];
            }
            set {
                _devices[index] = value;
                Reroute();
            }
        }

        public int Count {
            get {
                return _devices.Count;
            }
        }

        public Action<Signal> MIDIExit {
            get {
                return _midiexit;
            }
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public Chain Clone() {
            Chain ret = new Chain();
            foreach (Device device in _devices)
                ret.Add(device.Clone());
            return ret;
        }

        public void Insert(int index, Device device) {
            _devices.Insert(index, device);
            Reroute();
        }

        public void Add(Device device) {
            _devices.Add(device);
            Reroute();
        }

        public void Add(Device[] devices) {
            foreach (Device device in devices)
                _devices.Add(device);
            Reroute();
        }

        public void Remove(int index) {
            _devices.RemoveAt(index);
            Reroute();
        }

        public Chain() {}

        public Chain(Range zone) {
            Zone = zone;
        }

        public Chain(Device[] init) {
            _devices = init.ToList();
            Reroute();
        }

        public Chain(List<Device> init) {
            _devices = init;
            Reroute();
        }

        public Chain(Range zone, Device[] init) {
            _devices = init.ToList();
            Zone = zone;
            Reroute();
        }

        public Chain(Range zone, List<Device> init) {
            _devices = init;
            Zone = zone;
            Reroute();
        }

        public Chain(Action<Signal> exit) {
            _midiexit = exit;
            Reroute();
        }

        public Chain(Range zone, Action<Signal> exit) {
            Zone = zone;
            _midiexit = exit;
            Reroute();
        }

        public Chain(Device[] init, Action<Signal> exit) {
            _devices = init.ToList();
            _midiexit = exit;
            Reroute();
        }

        public Chain(List<Device> init, Action<Signal> exit) {
            _devices = init;
            _midiexit = exit;
            Reroute();
        }


        public Chain(Range zone, Device[] init, Action<Signal> exit) {
            _devices = init.ToList();
            Zone = zone;
            _midiexit = exit;
            Reroute();
        }

        public Chain(Range zone, List<Device> init, Action<Signal> exit) {
            _devices = init;
            Zone = zone;
            _midiexit = exit;
            Reroute();
        }

        public void MIDIEnter(Signal n) {
            if (Zone.Check(n.Index))
                if (_chainenter != null)
                    _chainenter(n);
        }
    }
}