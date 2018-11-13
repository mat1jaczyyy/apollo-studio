using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Group: Device {
        private List<Chain> _chains = new List<Chain>();

        public Chain this[int index] {
            get {
                return _chains[index];
            }
            set {
                _chains[index] = value;
                _chains[index].MIDIExit = ChainExit;
            }
        }

        public int Count {
            get {
                return _chains.Count;
            }
        }

        public override Device Clone() {
            return new Group((from chain in _chains select chain.Clone()).ToList());
        }

        public void Insert(int index) {
            _chains.Insert(index, new Chain(ChainExit));
        }

        public void Insert(int index, Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Insert(index, chain);
        }

        public void Add() {
            _chains.Add(new Chain(ChainExit));
        }

        public void Add(Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Add(chain);  
        }

        public void Add(Chain[] chains) {
            foreach (Chain chain in chains) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }     
        }

        public void Remove(int index) {
            _chains.RemoveAt(index);
        }

        public Group() {}

        public Group(Chain[] init) {
            Add(init);
        }

        public Group(List<Chain> init) {
            Add(init.ToArray());
        }

        public Group(Action<Signal> exit) {
            MIDIExit = exit;
        }

        public Group(Chain[] init, Action<Signal> exit) {
            Add(init);
            MIDIExit = exit;
        }

        public Group(List<Chain> init, Action<Signal> exit) {
            Add(init.ToArray());
            MIDIExit = exit;
        }

        private void ChainExit(Signal n) {
            if (MIDIExit != null)
                MIDIExit(n);
        }

        public override void MIDIEnter(Signal n) {
            foreach (Chain chain in _chains)
                chain.MIDIEnter(n.Clone());
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "group") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<Chain> init = new List<Chain>();
            for (int i = 0; i < Convert.ToInt32(data["count"]); i++) {
                init.Add(Chain.Decode(data[i.ToString()].ToString()));
            }
            return new Group(init);
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("group");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("count");
                        writer.WriteValue(_chains.Count);

                        for (int i = 0; i < _chains.Count; i++) {
                            writer.WritePropertyName(i.ToString());
                            writer.WriteRawValue(_chains[i].Encode());
                        }

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RequestSpecific(string jsonString) {
            throw new NotImplementedException();
        }
    }
}