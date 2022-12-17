using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Apollo.Core;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Rendering;
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
        
        public ISelect IClone(PurposeType purpose) => (ISelect)Clone(purpose);

        public DeviceViewer Viewer { get; set; }
        
        public Chain Parent;
        public int? ParentIndex;
        public PurposeType Purpose;

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

        protected abstract object[] CloneParameters(PurposeType purpose);

        public Device Clone(PurposeType purpose) {
            if (purpose == PurposeType.Unknown)
                Program.Log("WARNING: Purpose is unknown while cloning device!");

            Device device = Create(this.GetType(), purpose, CloneParameters(purpose));

            device.Collapsed = Collapsed;
            device.Enabled = Enabled;

            return device;
        }
        
        protected Device(string identifier, string name = null) {
            string s = $"constructing device {identifier}";
            Program.Log(s);
            if (Purpose == PurposeType.Unknown)
                throw new Exception($"Purpose is unknown while {s}");

            DeviceIdentifier = identifier;
            Name = name?? this.GetType().ToString().Split(".").Last();
        }

        bool ListeningToProjectLoaded = false;

        protected virtual void Initialized() {}

        public void Initialize() {
            if (Purpose == PurposeType.Unknown)
                throw new Exception("Purpose is unknown while initializing device!");

            if (Purpose == PurposeType.Passive)
                return;

            if (!Disposed) {
                if (Purpose == PurposeType.Unrelated) {
                    Initialized();
                    return;
                }

                if (Program.Project == null) {
                    Program.ProjectLoaded += Initialize;
                    ListeningToProjectLoaded = true;
                    return;
                }

                if (Track.Get(this) != null)
                    Initialized();
            }

            if (ListeningToProjectLoaded) {
                Program.ProjectLoaded -= Initialize;
                ListeningToProjectLoaded = false;
            }
        }

        public void InvokeExit(List<Signal> n) {
            if (!(n is StopSignal) && !n.Any()) return;

            Viewer?.Indicator.Trigger(n);
            MIDIExit?.Invoke(n);
        }

        public abstract void MIDIProcess(List<Signal> n);

        public override void MIDIEnter(List<Signal> n) {
            if (Disposed) return;

            if (n is StopSignal) Stop();
            else if (Enabled) {
                MIDIProcess(n);
                return;
            }
            
            InvokeExit(n);
        }

        protected void Stop() {
            jobs.Clear();
            Stopped();
        }

        protected virtual void Stopped() {}

        ConcurrentHashSet<Action> jobs = new();

        protected void Schedule(Action job, double time) {
            void Job() {
                if (!jobs.Contains(Job)) return;
                jobs.Remove(Job);

                job.Invoke();
            };

            jobs.Add(Job);
            Heaven.Schedule(Job, time);
        }

        public bool Disposed { get; private set; } = false;

        public virtual void Dispose() {
            if (Disposed) return;

            MIDIExit = null;
            Viewer = null;
            Parent = null;
            ParentIndex = null;
            
            Disposed = true;
        }

        public static Device Create(Type device, PurposeType purpose, object[] parameters = null) {
            object obj = FormatterServices.GetUninitializedObject(device);
            device.GetField("Purpose").SetValue(obj, purpose);

            ConstructorInfo ctor = device.GetConstructors()[0];
            int cnt = ctor.GetParameters().Length;

            if (parameters != null && parameters.Length != cnt)
                throw new Exception($"Expected {cnt} parameters, got {parameters.Length} for type {device}");

            ctor.Invoke(
                obj,
                BindingFlags.OptionalParamBinding,
                null, parameters?? Enumerable.Repeat(Type.Missing, ctor.GetParameters().Count()).ToArray(),
                CultureInfo.CurrentCulture
            );
            
            return (Device)obj;
        }

        public static T Create<T>(PurposeType purpose, object[] parameters = null) where T: Device
            => (T)Create(typeof(T), purpose, parameters);
    }
}