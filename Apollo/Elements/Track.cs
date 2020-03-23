using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Undo;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Track: ISelect, IChainParent, IMutable {
        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => Program.Project;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }
        
        public ISelect IClone() => (ISelect)Clone();

        public TrackInfo Info;
        public TrackWindow Window;
        
        public bool IsDisposing { get; private set; } = false;

        public delegate void ParentIndexChangedEventHandler(int index);
        public event ParentIndexChangedEventHandler ParentIndexChanged;
        
        public delegate void NameChangedEventHandler(string name);
        public event NameChangedEventHandler NameChanged;

        public delegate void DisposingEventHandler();
        public event DisposingEventHandler Disposing;

        int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                if (_ParentIndex != value) {
                    _ParentIndex = value;
                    ParentIndexChanged?.Invoke(_ParentIndex.Value);
                }
            }
        }

        public static Track Get(Device device) => (device.Parent?.Parent != null)
            ? ((device.Parent?.Parent is Track track)
                ? track
                : Get((Device)device.Parent?.Parent)
            ) : null;

        public static Track Get(Chain chain) => (chain.Parent != null)
            ? ((chain.Parent is Track track)
                ? track
                : Get((Device)chain.Parent)
            ) : null;
            
        public static bool PathContains(ISelect child, List<ISelect> search) {
            ISelect last = child;

            while (true) {
                if (last is Chain chain && chain.IRoot)
                    last = (ISelect)chain.Parent;

                if (search.Contains(last)) return true;

                if (last is Track) return false;
                
                last = (last is Chain _chain && _chain.Parent is Choke)  // Choke isn't an ISelectParent so IParent won't work!
                    ? (ISelect)((Chain)last).Parent
                    : (ISelect)last.IParent;
            }
        }

        public Chain Chain;
        Launchpad _launchpad;

        public Launchpad Launchpad {
            get => _launchpad;
            set {
                if (_launchpad != null) _launchpad.Receive -= MIDIEnter;

                if (value == null) value = MIDI.NoOutput;
                _launchpad = value;

                if (_launchpad != null) _launchpad.Receive += MIDIEnter;

                Info?.UpdatePorts();
            }
        }
        
        string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                NameChanged?.Invoke(ProcessedName);
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

        bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;

                    Info?.SetEnabled();
                    Window?.SetEnabled();
                }
            }
        }

        public Track Clone() => new Track(Chain.Clone(), Launchpad, Name) {
            Enabled = Enabled
        };

        public Track(Chain init = null, Launchpad launchpad = null, string name = "Track #") {
            Chain = init?? new Chain();
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;

            Launchpad = launchpad;
            Name = name;
        }

        void ChainExit(Signal n) => n.Source?.Render(n);

        void MIDIEnter(Signal n) {
            if (ParentIndex != null && Enabled) Chain?.MIDIEnter(n);
        }

        public void Dispose() {
            IsDisposing = true;

            Disposing?.Invoke();
            Disposing = null;
            NameChanged = null;
            ParentIndexChanged = null;

            Window?.Close();
            Window = null;
            Info = null;
            _ParentIndex = null;
            
            Chain?.Dispose();
            Chain = null;

            if (Launchpad != null) Launchpad.Receive -= MIDIEnter;
        }

        public class LaunchpadChangedUndoEntry: SimpleIndexUndoEntry<Launchpad> {
            protected override void Action(int index, Launchpad element) => Program.Project[index].Launchpad = element;

            public LaunchpadChangedUndoEntry(Track track, Launchpad u, Launchpad r)
            : base($"{track.ProcessedName} Launchpad Changed to {r.Name}", track.ParentIndex.Value, u, r) {}
        }

        public class RenamedUndoEntry: SimpleUndoEntry<List<string>> {
            int left, right;

            protected override void Action(List<string> element) {
                for (int i = left; i <= right; i++)
                    Program.Project[i].Name = element[i - left];
                
                Program.Project.Window?.Selection.Select(Program.Project[left]);
                Program.Project.Window?.Selection.Select(Program.Project[right], true);
            }
            
            public RenamedUndoEntry(int left, int right, List<string> u, List<string> r)
            : base($"Track Renamed to {r[0]}", u, r) {
                this.left = left;
                this.right = right;
            }
        }
    }
}