using System;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public class Chain: ISelect, ISelectParent {
        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => (Parent is ISelectParent)? (ISelectParent)Parent : null;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }

        public ISelectParentViewer IViewer {
            get => Viewer;
        }

        public List<ISelect> IChildren {
            get => Devices.Select(i => (ISelect)i).ToList();
        }

        public bool IRoot { 
            get => Parent.GetType() == typeof(Track);
        }

        public ChainInfo Info;
        public ChainViewer Viewer;

        public IChainParent Parent = null;

        public delegate void ParentIndexChangedEventHandler();
        public event ParentIndexChangedEventHandler ParentIndexChanged;
        public void ClearParentIndexChanged() => ParentIndexChanged = null;

        private int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                if (_ParentIndex != value) {
                    _ParentIndex = value;
                    ParentIndexChanged?.Invoke();
                }
            }
        }

        private Action<Signal> _midiexit = null;
        public Action<Signal> MIDIExit {
            get => _midiexit;
            set {
                _midiexit = value;
                Reroute();
            }
        }

        public List<Device> Devices = new List<Device>();
        private Action<Signal> _chainenter = null;

        private void Reroute() {
            for (int i = 0; i < Devices.Count; i++) {
                Devices[i].Parent = this;
                Devices[i].ParentIndex = i;
            }
            
            if (Devices.Count == 0)
                _chainenter = _midiexit;

            else {
                _chainenter = Devices[0].MIDIEnter;
                
                for (int i = 1; i < Devices.Count; i++)
                    Devices[i - 1].MIDIExit = Devices[i].MIDIEnter;
                
                Devices[Devices.Count - 1].MIDIExit = _midiexit;
            }
        }

        public Device this[int index] {
            get => Devices[index];
        }

        public int Count {
            get => Devices.Count;
        }

        private string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                Info?.SetName(_name);
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

        private bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;

                    Info?.SetEnabled();
                }
            }
        }

        public Chain Clone() => new Chain((from i in Devices select i.Clone()).ToList(), Name) {
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

        public Chain(List<Device> init = null, string name = "Chain #") {
            Devices = init?? new List<Device>();
            Name = name;
            Reroute();
        }

        public void MIDIEnter(Signal n) {
            if (Enabled) _chainenter?.Invoke(n);
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

        public static bool Move(List<Chain> source, IMultipleChainParent target, int position, bool copy = false) => (position == -1)
            ? Move(source, target, copy)
            : Move(source, target[position], copy);

        public static bool Move(List<Chain> source, Chain target, bool copy = false) {
            if (!copy && (source.Contains(target) || source[0].ParentIndex == target.ParentIndex + 1))
                return false;
            
            List<Chain> moved = new List<Chain>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) ((IMultipleChainParent)source[i].Parent).Remove(source[i].ParentIndex.Value, false);

                moved.Add(copy? source[i].Clone() : source[i]);

                ((IMultipleChainParent)target.Parent).Insert(target.ParentIndex.Value + i + 1, moved.Last());
            }

            Track track = Track.Get(moved.First());
            track?.Window?.Selection.Select(moved.First());
            track?.Window?.Selection.Select(moved.Last(), true);

            ((IMultipleChainParent)target.Parent).SpecificViewer.Expand(moved.Last().ParentIndex);
            
            return true;
        }

        public static bool Move(List<Chain> source, IMultipleChainParent target, bool copy = false) {
            if (!copy && target.Count > 0 && source[0] == target[0])
                return false;
            
            List<Chain> moved = new List<Chain>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) ((IMultipleChainParent)source[i].Parent).Remove(source[i].ParentIndex.Value, false);

                moved.Add(copy? source[i].Clone() : source[i]);

                target.Insert(i, moved.Last());
            }

            Track track = Track.Get(moved.First());
            track.Window.Selection.Select(moved.First());
            track.Window.Selection.Select(moved.Last(), true);

            target.SpecificViewer.Expand(moved.Last().ParentIndex);
            
            return true;
        }
    }
}