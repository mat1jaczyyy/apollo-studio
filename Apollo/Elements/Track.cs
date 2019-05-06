using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Structures;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Track: ISelect, IChainParent {
        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => Program.Project;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }

        public TrackInfo Info;
        public TrackWindow Window;

        public delegate void ParentIndexChangedEventHandler(int index);
        public event ParentIndexChangedEventHandler ParentIndexChanged;

        public delegate void DisposingEventHandler();
        public event DisposingEventHandler Disposing;

        private int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                if (_ParentIndex != value) {
                    _ParentIndex = value;
                    ParentIndexChanged?.Invoke(_ParentIndex.Value);
                }
            }
        }

        public static Track Get(Device device) => (device.Parent.Parent.GetType() == typeof(Track))? (Track)device.Parent.Parent : Get((Device)device.Parent.Parent);
        public static Track Get(Chain chain) => (chain.Parent.GetType() == typeof(Track))? (Track)chain.Parent : Get((Device)chain.Parent);

        public Chain Chain;
        private Launchpad _launchpad;

        public Launchpad Launchpad {
            get => _launchpad;
            set {
                if (_launchpad != null) _launchpad.Receive -= MIDIEnter;

                _launchpad = value;

                if (_launchpad != null) _launchpad.Receive += MIDIEnter;
            }
        }

        public Track Clone() => new Track(Chain.Clone());

        public Track(Chain init = null, Launchpad launchpad = null) {
            Chain = init?? new Chain();
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;

            Launchpad = launchpad;
        }

        private void ChainExit(Signal n) => n.Source?.Render(n);

        private void MIDIEnter(Signal n) => Chain?.MIDIEnter(n);

        public void Dispose() {
            Disposing?.Invoke();

            Window?.Close();
            Window = null;
            
            Chain?.Dispose();
            Chain = null;

            if (Launchpad != null) Launchpad.Receive -= MIDIEnter;
        }

        public static bool Move(List<Track> source, Track target, bool copy = false) {
            if (!copy)
                for (int i = 0; i < source.Count; i++)
                    if (source[i] == target) return false;
            
            List<Track> moved = new List<Track>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) {
                    Program.Project.Window.Contents_Remove(source[i].ParentIndex.Value);
                    Program.Project.Remove(source[i].ParentIndex.Value, false);
                }

                moved.Add(copy? source[i].Clone() : source[i]);

                Program.Project.Insert(target.ParentIndex.Value + i + 1, moved.Last());
                Program.Project.Window.Contents_Insert(target.ParentIndex.Value + i + 1, moved.Last());
            }

            Program.Project.Window.Selection.Select(moved.First());
            Program.Project.Window.Selection.Select(moved.Last(), true);
            
            return true;
        }

        public static bool Move(List<Track> source, Project target, bool copy = false) {
            if (!copy)
                if (target.Count > 0 && source[0] == target[0]) return false;
            
            List<Track> moved = new List<Track>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) {
                    Program.Project.Window.Contents_Remove(source[i].ParentIndex.Value);
                    Program.Project.Remove(source[i].ParentIndex.Value, false);
                }

                moved.Add(copy? source[i].Clone() : source[i]);

                Program.Project.Insert(i, moved.Last());
                Program.Project.Window.Contents_Insert(i, moved.Last());
            }

            Program.Project.Window.Selection.Select(moved.First());
            Program.Project.Window.Selection.Select(moved.Last(), true);
            
            return true;
        }
    }
}