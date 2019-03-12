using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Apollo.Components;
using Apollo.Core;

namespace Apollo.Elements {
    public class Chain: IDeviceParent, IResponse {
        public static readonly string Identifier = "chain";

        public IChainParent Parent = null;
        public int? ParentIndex;
        private Action<Signal> _midiexit = null;
        public Action<Signal> MIDIExit {
            get {
                return _midiexit;
            }
            set {
                _midiexit = value;
                Reroute();
            }
        }

        private List<Device> _devices = new List<Device>();
        private Action<Signal> _chainenter = null;

        private void Reroute() {
            for (int i = 0; i < _devices.Count; i++) {
                _devices[i].Parent = this;
                _devices[i].ParentIndex = i;
            }
            
            if (_devices.Count == 0)
                _chainenter = _midiexit;

            else {
                _chainenter = _devices[0].MIDIEnter;
                
                for (int i = 1; i < _devices.Count; i++) {
                    _devices[i - 1].MIDIExit = _devices[i].MIDIEnter;
                }
                
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

        public Chain Clone() {
            return new Chain((from i in _devices select i.Clone()).ToList());
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

        public Chain(List<Device> init = null) {
            if (init == null) init = new List<Device>();
            _devices = init;
            Reroute();
        }

        public void MIDIEnter(Signal n) {
            _chainenter?.Invoke(n);
        }

        public static Chain Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            List<object> data = JsonConvert.DeserializeObject<List<object>>(json["data"].ToString());
            
            List<Device> init = new List<Device>();
            foreach (object device in data) {
                init.Add(Device.Decode(device.ToString()));
            }
            return new Chain(init);
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < _devices.Count; i++) {
                            writer.WriteRawValue(_devices[i].Encode());
                        }

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public string Request(Dictionary<string, object> data, List<string> path = null) {
            if (path == null) path = new List<string>();
            path.Insert(0, Identifier);

            if (ParentIndex != null)
                path[0] += $":{ParentIndex}";

            return Parent.Request(data, path);
        }

        public ObjectResult Respond(string obj, string[] path, Dictionary<string, object> data) {
            if (!path[0].StartsWith(Identifier)) return new BadRequestObjectResult("Incorrect recipient for message.");

            if (path.Count() > 1) {
                if (path[1].StartsWith("device:"))
                    return _devices[Convert.ToInt32(path[1].Split(':')[1])].Respond(obj, path.Skip(1).ToArray(), data);

                else return new BadRequestObjectResult("Incorrectly formatted message.");
            }

            switch (data["type"].ToString()) {
                case "add":
                    foreach (Type device in (from type in Assembly.GetExecutingAssembly().GetTypes() where (type.Namespace.StartsWith("Apollo.Devices") && !type.Namespace.StartsWith("Apollo.Devices.Device")) select type)) {
                        if (device.Name.ToLower().Equals(data["device"])) {
                            Insert(Convert.ToInt32(data["index"]), (Device)Activator.CreateInstance(device, BindingFlags.OptionalParamBinding, null, new object[0], CultureInfo.CurrentCulture));
                            return new OkObjectResult(_devices[Convert.ToInt32(data["index"])].Encode());
                        }
                    }
                    return new BadRequestObjectResult("Incorrectly formatted message.");

                case "remove":
                    Remove(Convert.ToInt32(data["index"]));
                    return new OkObjectResult(null);
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}