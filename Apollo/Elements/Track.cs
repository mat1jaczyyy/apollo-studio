using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Components;

namespace Apollo.Elements {
    public class Track: Window, IChainParent, IResponse {
        public static readonly string Identifier = "track";
        
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        public int? ParentIndex;

        public Chain Chain;
        private Launchpad _launchpad;
        private Pixel[] screen = new Pixel[128];

        public Launchpad Launchpad {
            get {
                return _launchpad;
            }
            set {
                if (_launchpad != null)
                    _launchpad.Receive -= MIDIEnter;

                _launchpad = value;

                if (_launchpad != null)
                    _launchpad.Receive += MIDIEnter;
            }
        }

        public Track(Chain init = null, Launchpad launchpad = null) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            if (init == null) init = new Chain();
            Chain = init;
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;
            
            for (int i = 0; i < 128; i++)
                screen[i] = new Pixel() {MIDIExit = MIDIExit};

            Launchpad = launchpad;
        }

        private void ChainExit(Signal n) {
            screen[n.Index].MIDIEnter(n);
        }

        private void MIDIExit(Signal n) {
            Launchpad.Send(n);
        }

        private void MIDIEnter(Signal n) {
            if (Chain != null)
                Chain.MIDIEnter(n);
        }

        public void Dispose() {
            if (Launchpad != null)
                Launchpad.Receive -= MIDIEnter;
            
            Chain = null;
        }

        public override string ToString() {
            return Launchpad.Name;
        }
        
        public static Track Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Track(Chain.Decode(data["chain"].ToString()), Launchpad.Decode(data["launchpad"].ToString()));
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

        public string Request(Dictionary<string, object> data, List<string> path = null) {
            if (path == null) path = new List<string>();
            path.Insert(0, Identifier);

            if (ParentIndex != null)
                path[0] += $":{ParentIndex}";

            return Set.Request(data, path);
        }

        public ObjectResult Respond(string obj, string[] path, Dictionary<string, object> data) {
            if (!path[0].StartsWith(Identifier)) return new BadRequestObjectResult("Incorrect recipient for message.");

            if (path.Count() > 1) {
                if (path[1] == "chain")
                    return Chain.Respond(obj, path.Skip(1).ToArray(), data);

                else return new BadRequestObjectResult("Incorrectly formatted message.");
            }

            switch (data["type"].ToString()) {
                case "port":
                    Launchpad = MIDI.Devices[Convert.ToInt32(data["index"])];
                    return new OkObjectResult(Launchpad.Encode());
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}