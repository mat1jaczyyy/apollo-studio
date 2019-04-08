using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Track: IChainParent {
        public static readonly string Identifier = "track";

        public TrackWindow Window;

        public delegate void ParentIndexChangedEventHandler(int index);
        public event ParentIndexChangedEventHandler ParentIndexChanged;

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

        public Chain Chain;
        private Launchpad _launchpad;
        private Pixel[] screen = new Pixel[100];

        public Launchpad Launchpad {
            get => _launchpad;
            set {
                if (_launchpad != null) _launchpad.Receive -= MIDIEnter;

                _launchpad = value;

                if (_launchpad != null) _launchpad.Receive += MIDIEnter;
            }
        }

        public Track(Chain init = null, Launchpad launchpad = null) {
            if (init == null) init = new Chain();
            Chain = init;
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;
            
            for (int i = 0; i < 100; i++)
                screen[i] = new Pixel() {MIDIExit = MIDIExit};

            Launchpad = launchpad;
        }

        private void ChainExit(Signal n) => screen[n.Index].MIDIEnter(n);

        private void MIDIExit(Signal n) => Launchpad.Send(n);

        private void MIDIEnter(Signal n) => Chain?.MIDIEnter(n);

        public void Dispose() {
            if (Launchpad != null)
                Launchpad.Receive -= MIDIEnter;
            
            Chain = null;
        }
        
        public static Track Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Track(
                Chain.Decode(data["chain"].ToString()),
                Launchpad.Decode(data["launchpad"].ToString())
            );
        }

        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("chain");
                        writer.WriteRawValue(Chain.Encode());

                        writer.WritePropertyName("launchpad");
                        writer.WriteRawValue(Launchpad.Encode());

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}