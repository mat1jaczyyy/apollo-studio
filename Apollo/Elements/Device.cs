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
        public Action<Signal> MIDIExit = null;

        public abstract Device Clone();
        
        protected Device(string device) => DeviceIdentifier = device;

        public abstract void MIDIEnter(Signal n);

        public virtual void Dispose() => MIDIExit = null;

        public bool Move(Device device, bool copy = false) {
            if (!copy) {
                if (this == device) return false;

                Parent.Viewer.Contents_Remove(ParentIndex.Value);
                Parent.Remove(ParentIndex.Value, false);
            }

            Device moving = copy? Clone() : this;

            device.Parent.Viewer.Contents_Insert(device.ParentIndex.Value + 1, moving);
            device.Parent.Insert(device.ParentIndex.Value + 1, moving);

            moving.Parent.Viewer.Select(moving.ParentIndex);
            return true;
        }

        public bool Move(Chain chain, bool copy = false) {
            if (!copy) {
                if (chain.Count > 0 && this == chain[0]) return false;

                Parent.Viewer.Contents_Remove(ParentIndex.Value);
                Parent.Remove(ParentIndex.Value);
            }

            Device moving = copy? Clone() : this;

            chain.Viewer.Contents_Insert(0, moving);
            chain.Insert(0, moving);

            moving.Parent.Viewer.Select(moving.ParentIndex);
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