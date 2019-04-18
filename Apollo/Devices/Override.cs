using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Override: Device {
        public static readonly new string DeviceIdentifier = "override";

        public delegate void TargetChangedEventHandler(int value);
        public event TargetChangedEventHandler TargetChanged;
        
        private int _target;
        public int Target {
            get => _target;
            set {
                if (_target != value) {
                    Program.Project.Tracks[_target].ParentIndexChanged -= IndexChanged;
                    Program.Project.Tracks[_target].Disposed -= IndexRemoved;
                    
                    _target = value;
                    Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
                    Program.Project.Tracks[_target].Disposed += IndexRemoved;
                }
            }
        }

        private void IndexChanged(int value) {
            _target = value;
            TargetChanged?.Invoke(_target);
        }

        private void IndexRemoved() {
            Target = Track.Get(this).ParentIndex.Value;
            TargetChanged?.Invoke(_target);
        }

        public Launchpad Launchpad => Program.Project.Tracks[Target].Launchpad;

        public override Device Clone() => new Override {Parent = new Chain()};

        public Override(int target = -1): base(DeviceIdentifier) {
            if (target < 0) target = Track.Get(this).ParentIndex.Value;
            _target = target;

            if (Program.Project == null) Program.ProjectLoaded += Initialize;
            else Initialize();
        }

        private void Initialize() {
            Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
            Program.Project.Tracks[_target].Disposed += IndexRemoved;
        }

        public override void MIDIEnter(Signal n) {
            n.Source = Launchpad;
            MIDIExit?.Invoke(n);
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            return new Override(
                Convert.ToInt32(data["target"])
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

                        writer.WritePropertyName("target");
                        writer.WriteValue(Target);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}