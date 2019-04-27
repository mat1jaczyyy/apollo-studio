using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using Newtonsoft.Json;

using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public abstract class Device {
        public static readonly string Identifier = "device";
        public readonly string DeviceIdentifier;

        public DeviceViewer Viewer;

        public Chain Parent;
        public int? ParentIndex;
        public virtual Action<Signal> MIDIExit { get; set; } = null;

        public abstract Device Clone();
        
        protected Device(string device) => DeviceIdentifier = device;

        public abstract void MIDIEnter(Signal n);

        public virtual void Dispose() => MIDIExit = null;

        public static bool Move(List<Device> source, Device target, bool copy = false) {
            if (!copy)
                for (int i = 0; i < source.Count; i++)
                    if (source[i] == target) return false;
            
            List<Device> moved = new List<Device>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) {
                    source[i].Parent.Viewer.Contents_Remove(source[i].ParentIndex.Value);
                    source[i].Parent.Remove(source[i].ParentIndex.Value, false);
                }

                moved.Add(copy? source[i].Clone() : source[i]);

                target.Parent.Viewer.Contents_Insert(target.ParentIndex.Value + i + 1, moved.Last());
                target.Parent.Insert(target.ParentIndex.Value + i + 1, moved.Last());
            }

            Track track = Track.Get(moved.First());
            track.Window.Select(moved.First());
            track.Window.Select(moved.Last(), true);
            
            return true;
        }

        public static bool Move(List<Device> source, Chain target, bool copy = false) {
            if (!copy)
                if (target.Count > 0 && source[0] == target[0]) return false;
            
            List<Device> moved = new List<Device>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) {
                    source[i].Parent.Viewer.Contents_Remove(source[i].ParentIndex.Value);
                    source[i].Parent.Remove(source[i].ParentIndex.Value, false);
                }

                moved.Add(copy? source[i].Clone() : source[i]);

                target.Viewer.Contents_Insert(i, moved.Last());
                target.Insert(i, moved.Last());
            }

            Track track = Track.Get(moved.First());
            track.Window.Select(moved.First());
            track.Window.Select(moved.Last(), true);
            
            return true;
        }

        public static Device Create(Type device, Chain parent) {
            object obj = FormatterServices.GetUninitializedObject(device);
            device.GetField("Parent").SetValue(obj, parent);

            ConstructorInfo ctor = device.GetConstructors()[0];
            ctor.Invoke(
                obj,
                BindingFlags.OptionalParamBinding,
                null, Enumerable.Repeat(Type.Missing, ctor.GetParameters().Count()).ToArray(),
                CultureInfo.CurrentCulture
            );
            
            return (Device)obj;
        }

        public static Device Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return null;

            object[] specific = new object[] {json["data"].ToString()};
            
            foreach (Type device in (from type in Assembly.GetExecutingAssembly().GetTypes() where type.Namespace.StartsWith("Apollo.Devices") select type)) {
                object parsed = device.GetMethod("DecodeSpecific").Invoke(null, specific);
                if (parsed != null) return (Device)parsed;
            }

            return null;
        }

        public abstract string EncodeSpecific();
        public string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteRawValue(EncodeSpecific());

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}