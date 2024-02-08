﻿using System.Collections.Generic;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements.Launchpads;
using Apollo.Elements.Purpose;
using Apollo.Enums;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Rendering;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Track: ISelect, IChainParent, IMutable, IName, IInitializable {
        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => Program.Project;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }
        
        public ISelect IClone(PurposeType purpose) => (ISelect)Clone(purpose);

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

        public PurposeType Purpose { get; protected set; }

        public Chain Chain;
        Launchpad _launchpad;

        public void SetLaunchpadWithoutReceive(Launchpad value) {
            if (_launchpad != null) _launchpad.Receive -= MIDIEnter;

            if (value == null) value = MIDI.NoOutput;
            _launchpad = value;

            Info?.PortSelector.Update(_launchpad);
        }

        void AddReceive() {
            if (Disposed) return;
            
            if (_launchpad != null) _launchpad.Receive += MIDIEnter;
        }

        public Launchpad Launchpad {
            get => _launchpad;
            set {
                SetLaunchpadWithoutReceive(value);
                AddReceive();
            }
        }
        
        string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                NameChanged?.Invoke(ProcessedName);
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
                    Window?.SetEnabled();
                }
            }
        }

        public Track Clone(PurposeType purpose) => new Track(purpose, Chain.Clone(purpose), Launchpad, Name) {
            Enabled = Enabled
        };

        public Track(PurposeType purpose, Chain init = null, Launchpad launchpad = null, string name = "Track #") {
            Purpose = purpose;

            Chain = init?? new Chain();
            Chain.Parent = this;
            Chain.MIDIExit = Heaven.MIDIEnter;

            SetLaunchpadWithoutReceive(launchpad);
            Name = name;

            if (_launchpad != null)
                (this as IInitializable).Initialize();
        }

        public void Initialized() {
            AddReceive();
        }

        void MIDIEnter(Signal n) {
            if (!Disposed && ParentIndex != null && Enabled) {
                try {
                    Chain?.MIDIEnter(n.Clone());
                } catch (KeyNotFoundException) {} // Ignore KeyNotFound race conditions in devices that have dictionaries
            }
        }

        public bool Disposed { get; private set; } = false;

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

            Disposed = true;
        }
    }
}