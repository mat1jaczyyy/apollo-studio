using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Multi: Device, IChainParent {
        public static readonly new string DeviceIdentifier = "multi";

        private Action<Signal> _midiexit;
        public override Action<Signal> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public Chain Preprocess;
        private List<Chain> _chains = new List<Chain>();

        private Random RNG = new Random();
        public bool Random;

        private int current = -1;
        private ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();

        private void Reroute() {
            Preprocess.Parent = this;
            Preprocess.MIDIExit = PreprocessExit;

            for (int i = 0; i < _chains.Count; i++) {
                _chains[i].Parent = this;
                _chains[i].ParentIndex = i;
                _chains[i].MIDIExit = ChainExit;
            }
        }

        public Chain this[int index] {
            get => _chains[index];
        }

        public int Count {
            get => _chains.Count;
        }

        public override Device Clone() => new Multi(Preprocess.Clone(), (from i in _chains select i.Clone()).ToList(), Random, Expanded);

        public void Insert(int index, Chain chain = null) {
            _chains.Insert(index, chain?? new Chain());
            
            Reroute();
        }

        public void Add(Chain chain) {
            _chains.Add(chain);

            Reroute();
        }

        public void Remove(int index) {
            _chains.RemoveAt(index);

            Reroute();
        }

        private void Reset() => current = -1;

        public int? Expanded;

        public Multi(Chain preprocess = null, List<Chain> init = null, bool random = false, int? expanded = null): base(DeviceIdentifier) {
            Preprocess = preprocess?? new Chain();

            foreach (Chain chain in init?? new List<Chain>()) _chains.Add(chain);

            Random = random;
            
            Expanded = expanded;
            
            Launchpad.MultiReset += Reset;

            Reroute();
        }

        private void ChainExit(Signal n) => MIDIExit?.Invoke(n);

        public override void MIDIEnter(Signal n) {
            Signal m = n.Clone();
            n.Color = new Color();

            if (!buffer.ContainsKey(n)) {
                if (!m.Color.Lit) return;

                if (Random) current = RNG.Next(_chains.Count);
                else if (++current >= _chains.Count) current = 0;

                m.MultiTarget = buffer[n] = current;

            } else {
                m.MultiTarget = buffer[n];
                if (!m.Color.Lit) buffer.Remove(n, out int _);
            }

            Preprocess.MIDIEnter(m);
        }

        private void PreprocessExit(Signal n) {
            int target = n.MultiTarget.Value;
            n.MultiTarget = null;
            
            if (_chains.Count == 0) {
                MIDIExit?.Invoke(n);
                return;
            }
            
            _chains[target].MIDIEnter(n);
        }

        public override void Dispose() {
            Preprocess.Dispose();
            foreach (Chain chain in _chains) chain.Dispose();
            base.Dispose();
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> chains = JsonConvert.DeserializeObject<List<object>>(data["chains"].ToString());
            List<Chain> init = new List<Chain>();

            foreach (object chain in chains)
                init.Add(Chain.Decode(chain.ToString()));
            
            return new Multi(
                Chain.Decode(data["preprocess"].ToString()),
                init,
                Convert.ToBoolean(data["random"].ToString()),
                int.TryParse(data["expanded"].ToString(), out int i)? (int?)i : null
            );
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("preprocess");
                        writer.WriteRawValue(Preprocess.Encode());

                        writer.WritePropertyName("chains");
                        writer.WriteStartArray();

                            for (int i = 0; i < _chains.Count; i++)
                                writer.WriteRawValue(_chains[i].Encode());

                        writer.WriteEndArray();

                        writer.WritePropertyName("random");
                        writer.WriteValue(Random);

                        writer.WritePropertyName("expanded");
                        writer.WriteValue(Expanded);
                        
                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}