using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public class Track: Window, IChainParent {
        public static readonly string Identifier = "track";
        
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        private int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                _ParentIndex = value;
                if (Program.Project != null) {
                    this.Get<TextBlock>("Title").Text = (Program.Project.FilePath == "")
                        ? $"Track {ParentIndex + 1} - Untitled"
                        : $"Track {ParentIndex + 1} - {Program.Project.FilePath}";
                }
            }
        }

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
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));

            if (init == null) init = new Chain();
            Chain = init;
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;
            
            for (int i = 0; i < 128; i++)
                screen[i] = new Pixel() {MIDIExit = MIDIExit};

            Launchpad = launchpad;

            this.Get<ScrollViewer>("Contents").Content = new ChainViewer(Chain);
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
    }
}