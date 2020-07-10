using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Apollo.Selection;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public abstract class Device: SignalReceiver, ISelect, IMutable {
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

        public bool Collapsed = false;
    
        protected IEnumerable<Signal> InvokeExit(IEnumerable<Signal> n) {
            //Viewer?.Indicator.Trigger(n.Color.Lit); TODO Heaven indicators
            return MIDIExit.Invoke(n);
        }
        
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

        public abstract IEnumerable<Signal> MIDIProcess(IEnumerable<Signal> n);

        public override IEnumerable<Signal> MIDIEnter(IEnumerable<Signal> n) {
            n = n.Select(i => i.Clone());

            if (Disposed) return n;

            if (n.FirstOrDefault() is StopSignal) Stop();
            else if (Enabled)
                return MIDIProcess(n).ToList();
            
            return n;
        }

        public IEnumerable<Signal> ChainEnter(IEnumerable<Signal> n)
            => InvokeExit(MIDIEnter(n));

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