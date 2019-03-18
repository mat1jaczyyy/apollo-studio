using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Devices {
    public class Group: Device, IChainParent {
        public static readonly new string DeviceIdentifier = "group";

        private List<Chain> _chains = new List<Chain>();

        private void Reroute() {
            for (int i = 0; i < _chains.Count; i++) {
                _chains[i].Parent = this;
                _chains[i].ParentIndex = i;
            }
        }

        public Chain this[int index] {
            get {
                return _chains[index];
            }
        }

        public int Count {
            get {
                return _chains.Count;
            }
        }

        public override Device Clone() {
            return new Group((from i in _chains select i.Clone()).ToList());
        }

        public void Insert(int index) {
            _chains.Insert(index, new Chain() {MIDIExit = ChainExit});
            
            Reroute();
        }

        public void Insert(int index, Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Insert(index, chain);
            
            Reroute();
        }

        public void Add() {
            _chains.Add(new Chain() {Parent = this, ParentIndex = _chains.Count, MIDIExit = ChainExit});
        }

        public void Add(Chain chain) {
            chain.Parent = this;
            chain.ParentIndex = _chains.Count;
            chain.MIDIExit = ChainExit;
            _chains.Add(chain);
        }

        public void Add(List<Chain> chains) {
            foreach (Chain chain in chains) {
                chain.Parent = this;
                chain.ParentIndex = _chains.Count;
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }     
        }

        public void Remove(int index) {
            _chains.RemoveAt(index);

            Reroute();
        }

        public Group(List<Chain> init = null): base(DeviceIdentifier) {
            if (init == null) init = new List<Chain>();
            Add(init);
        }

        private void ChainExit(Signal n) {
            MIDIExit?.Invoke(n);
        }

        public override void MIDIEnter(Signal n) {
            foreach (Chain chain in _chains)
                chain.MIDIEnter(n.Clone());
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            List<object> data = JsonConvert.DeserializeObject<List<object>>(json["data"].ToString());
            
            List<Chain> init = new List<Chain>();
            foreach (object chain in data) {
                init.Add(Chain.Decode(chain.ToString()));
            }
            return new Group(init);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartArray();

                        for (int i = 0; i < _chains.Count; i++) {
                            writer.WriteRawValue(_chains[i].Encode());
                        }

                    writer.WriteEndArray();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}