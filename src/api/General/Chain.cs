using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using api.Devices;
using Newtonsoft.Json;

namespace api {
    public class Chain {
        private List<Device> _devices = new List<Device>();
        private Action<Signal> _chainenter = null;
        private Action<Signal> _midiexit = null;

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

        public Chain(Device[] init) {
            _devices = init.ToList();
            Reroute();
        }

        public Chain(List<Device> init) {
            _devices = init;
            Reroute();
        }

        public Chain(Action<Signal> exit) {
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

        public void MIDIEnter(Signal n) {
            if (_chainenter != null)
                _chainenter(n);
        }

        public static Chain Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "chain") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<Device> init = new List<Device>();
            Dictionary<string, object> devices = JsonConvert.DeserializeObject<Dictionary<string, object>>(data["devices"].ToString());
            for (int i = 0; i < Convert.ToInt32(devices["count"]); i++) {
                init.Add(Device.Decode(devices[i.ToString()].ToString()));
            }
            return new Chain(init);
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue("chain");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("count");
                        writer.WriteValue(_devices.Count);

                        for (int i = 0; i < _devices.Count; i++) {
                            writer.WritePropertyName(i.ToString());
                            writer.WriteRawValue(_devices[i].Encode());
                        }

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}