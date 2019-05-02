using Apollo.Core;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Track: IChainParent {
        public static readonly string Identifier = "track";

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
    }
}