using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Apollo.Devices;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public abstract class Device: ISelect {
        public readonly string DeviceIdentifier;
        public readonly string Name;

        public ISelectViewer IInfo {
            get => Viewer;
        }

        public ISelectParent IParent {
            get => Parent;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }
        
        public ISelect IClone() => (ISelect)Clone();

        public DeviceViewer Viewer { get; set; }
        
        public Chain Parent;
        public int? ParentIndex;
        public virtual Action<Signal> MIDIExit { get; set; } = null;

        protected void InvokeExit(Signal n) {
            Viewer?.Indicator.Trigger(n.Color.Lit);
            MIDIExit?.Invoke(n);
        }

        public bool Collapsed = false;
        
        bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;

                    Viewer?.SetEnabled();
                }
            }
        }

        public abstract Device Clone();
        
        protected Device(string identifier, string name = null) {
            DeviceIdentifier = identifier;
            Name = name?? this.GetType().ToString().Split(".").Last();
        }

        public abstract void MIDIProcess(Signal n);

        public void MIDIEnter(Signal n) {
            if (Disposed) return;

            if (n is StopSignal) Stop();
            else if (Enabled) {
                MIDIProcess(n);
                return;
            }
            
            InvokeExit(n);
        }

        protected virtual void Stop() {}

        public bool Disposed { get; private set; } = false;

        public virtual void Dispose() {
            if (Disposed) return;

            MIDIExit = null;
            Viewer = null;
            Parent = null;
            ParentIndex = null;
            
            Disposed = true;
        }

        public static bool Move(List<Device> source, Chain target, int position, bool copy = false) {
            if (!copy && Track.PathContains(target, source.Select(i => (ISelect)i).ToList())) return false;

            return (position == -1)
                ? Move(source, target, copy)
                : Move(source, target[position], copy);
        }

        public static bool Move(List<Device> source, Device target, bool copy = false) {
            if (!copy && (source.Contains(target) || (source[0].Parent == target.Parent && source[0].ParentIndex == target.ParentIndex + 1)))
                return false;
            
            List<Device> moved = new List<Device>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) source[i].Parent.Remove(source[i].ParentIndex.Value, false);

                moved.Add(copy? source[i].Clone() : source[i]);

                if (moved.Last() is Pattern pattern)
                    pattern.Window?.Close();

                target.Parent.Insert(target.ParentIndex.Value + i + 1, moved.Last());
            }

            Track track = Track.Get(moved.First());
            track?.Window?.Selection.Select(moved.First());
            track?.Window?.Selection.Select(moved.Last(), true);
            
            return true;
        }

        public static bool Move(List<Device> source, Chain target, bool copy = false) {
            if (!copy && target.Count > 0 && source[0] == target[0])
                return false;
            
            List<Device> moved = new List<Device>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) source[i].Parent.Remove(source[i].ParentIndex.Value, false);

                moved.Add(copy? source[i].Clone() : source[i]);

                if (moved.Last() is Pattern pattern)
                    pattern.Window?.Close();

                target.Insert(i, moved.Last());
            }

            Track track = Track.Get(moved.First());
            track.Window.Selection.Select(moved.First());
            track.Window.Selection.Select(moved.Last(), true);
            
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
    }
}