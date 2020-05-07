using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;

using Apollo.Devices;
using Apollo.DeviceViewers;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Undo;
using Apollo.Viewers;
using System.IO;
using Apollo.Binary;

namespace Apollo.Elements {
    public interface IChainParent: ISelect {}

    public class Chain: ISelect, ISelectParent, IMutable, IName {
        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => (Parent is ISelectParent)? (ISelectParent)Parent : null;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }

        public void IInsert(int index, ISelect item) => Insert(index, (Device)item);
        
        public ISelect IClone() => (ISelect)Clone();

        public ISelectParentViewer IViewer {
            get => Viewer;
        }

        public List<ISelect> IChildren {
            get => Devices.Select(i => (ISelect)i).ToList();
        }

        public bool IRoot { 
            get => Parent is Track;
        }
        
        public Window IWindow => Track.Get(this)?.Window;
        public SelectionManager Selection => Track.Get(this)?.Window?.Selection;

        public Type ChildType => typeof(Device);
        public string ChildString => "Device";
        public string ChildFileExtension => "apdev";

        public ChainInfo Info;
        public ChainViewer Viewer;

        public IChainParent Parent = null;

        public delegate void ParentIndexChangedEventHandler();
        public event ParentIndexChangedEventHandler ParentIndexChanged;
        public void ClearParentIndexChanged() => ParentIndexChanged = null;

        int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                if (_ParentIndex != value) {
                    _ParentIndex = value;
                    ParentIndexChanged?.Invoke();
                }
            }
        }

        Action<Signal> _midiexit = null;
        public Action<Signal> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        void InvokeExit(Signal n) {
            Info?.Indicator.Trigger(n.Color.Lit);
            MIDIExit?.Invoke(n);
        }

        public List<Device> Devices = new List<Device>();
        Action<Signal> _chainenter = null;

        void Reroute() {
            for (int i = 0; i < Devices.Count; i++) {
                Devices[i].Parent = this;
                Devices[i].ParentIndex = i;
            }
            
            if (Devices.Count == 0)
                _chainenter = InvokeExit;

            else {
                _chainenter = Devices[0].MIDIEnter;
                
                for (int i = 1; i < Devices.Count; i++)
                    Devices[i - 1].MIDIExit = Devices[i].MIDIEnter;
                
                Devices[Devices.Count - 1].MIDIExit = InvokeExit;
            }
        }

        public Device this[int index] {
            get => Devices[index];
        }

        public int Count {
            get => Devices.Count;
        }

        string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                Info?.Rename.SetName(_name);
            }
        }

        public string ProcessedName {
            get {
                string ret = "";
                for (int i = 0; i < _name.Length; i++)
                    ret += (_name[i] == '#' && (i == 0 || _name[i - 1] == ' ') && (i == _name.Length - 1 || _name[i + 1] == ' '))? (ParentIndex + 1).ToString() : _name[i].ToString();

                return ret;
            } 
        }

        bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;

                    Info?.SetEnabled();
                }
            }
        }

        bool[] _filter;
        public bool[] SecretMultiFilter {
            get => _filter;
            set {
                if (value.Length == 101) {
                    _filter = value;
                    if (Parent is Multi multi && multi.Viewer?.SpecificViewer != null) ((MultiViewer)multi.Viewer.SpecificViewer).Set(this, _filter);
                }
            }
        }

        public Chain Clone() => new Chain(Devices.Select(i => i.Clone()).ToList(), Name, SecretMultiFilter.ToArray()) {
            Enabled = Enabled
        };

        public void Insert(int index, Device device) {
            Devices.Insert(index, device);
            Reroute();

            Viewer?.Contents_Insert(index, Devices[index]);

            Track.Get(this)?.Window?.Selection.Select(Devices[index]);
        }
        
        public void Add(Device device) => Insert(Devices.Count, device);

        public void Remove(int index, bool dispose = true) {
            if (index < Devices.Count - 1)
                Track.Get(this).Window?.Selection.Select(Devices[index + 1]);
            else if (Devices.Count > 1)
                Track.Get(this).Window?.Selection.Select(Devices[Devices.Count - 2]);
            else
                Track.Get(this).Window?.Selection.Select(null);

            Viewer?.Contents_Remove(index);

            if (dispose) Devices[index].Dispose();
            Devices.RemoveAt(index);
            Reroute();
        }

        public Chain(List<Device> init = null, string name = "Chain #", bool[] filter = null) {
            Devices = init?? new List<Device>();
            Name = name;

            if (filter == null || filter.Length != 101) filter = new bool[101];
            _filter = filter;
            
            Reroute();
        }

        public void MIDIEnter(Signal n) {
            if (n is StopSignal) _chainenter?.Invoke(n);
            else if (Enabled) {
                Viewer?.Indicator.Trigger(n.Color.Lit);
                _chainenter?.Invoke(n);
            }
        }

        public void Dispose() {
            ParentIndexChanged = null;
            foreach (Device device in Devices) device.Dispose();
            MIDIExit = null;
            Info = null;
            Viewer = null;
            Parent = null;
            _ParentIndex = null;
        }
        
        public class DeviceInsertedUndoEntry: PathUndoEntry<Chain> {
            int index;
            Device device;

            protected override void UndoPath(params Chain[] items) => items[0].Remove(index);
            protected override void RedoPath(params Chain[] items) => items[0].Insert(index, device.Clone());
            
            protected override void OnDispose() => device.Dispose();
            
            public DeviceInsertedUndoEntry(Chain chain, int index, Device device)
            : base($"Device ({device.Name}) Inserted", chain) {
                this.index = index;
                this.device = device.Clone();
            }
            
            DeviceInsertedUndoEntry(BinaryReader reader, int version): base(reader, version){
                index = reader.ReadInt32();
                device = Decoder.DecodeAnything<Device>(reader, version);
            }
            
            public override void Encode(BinaryWriter writer){
                base.Encode(writer);
                
                writer.Write(index);
                Encoder.EncodeAnything(writer, device);
            }
        }
    }
}