using System;
using System.Collections.Generic;
using System.Linq;

using api.Devices;

namespace api {
    public class Chain {
        private List<Device> _devices;
        private Action<Signal> _chainenter;
        private Action<Signal> _midiexit;

        private void Reroute() {
            if (_devices.Count == 0) {
                this._chainenter = this._midiexit;
            } else {
                this._chainenter = this._devices[0].MIDIEnter;
                for (int i = 1; i < _devices.Count; i++) {
                    _devices[i - 1].MIDIExit = _devices[i].MIDIEnter;
                }
                _devices[_devices.Count - 1].MIDIExit = this._midiexit;
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
            return new Chain(_devices.ToArray());
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
            foreach (Device device in devices) {
                _devices.Add(device);
            }
            Reroute();
        }

        public void Remove(int index) {
            _devices.RemoveAt(index);
            Reroute();
        }

        public Chain() {
            _devices = new List<Device>();
            this.MIDIExit = null;
        }

        public Chain(Device[] init) {
            _devices = new List<Device>();
            foreach (Device device in init) {
                _devices.Add(device);
            }
            this.MIDIExit = null;
        }
        
        public Chain(List<Device> init) {
            _devices = init;
            this.MIDIExit = null;
        }

        public Chain(Action<Signal> exit) {
            _devices = new List<Device>();
            this.MIDIExit = exit;
        }

        public Chain(Device[] init, Action<Signal> exit) {
            _devices = new List<Device>();
            foreach (Device device in init) {
                _devices.Add(device);
            }
            this.MIDIExit = exit;
        }

        public Chain(List<Device> init, Action<Signal> exit) {
            _devices = init;
            this.MIDIExit = exit;
        }

        public void MIDIEnter(Signal n) {
            if (this._chainenter != null)
                this._chainenter(n);
        }
    }
}