using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Flip: Device {
        FlipType _mode;
        public FlipType Mode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((FlipViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }

        bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((FlipViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Mode, Bypass };

        public Flip(FlipType mode = FlipType.Horizontal, bool bypass = false): base("flip") {
            Mode = mode;
            Bypass = bypass;
        }

        public override void MIDIProcess(List<Signal> n)
            => InvokeExit((Bypass? n.Select(i => i.Clone()) : Enumerable.Empty<Signal>()).Concat(n.SelectMany(i => {
                if (i.Index == 100) 
                    return Bypass? Enumerable.Empty<Signal>() : new [] {i};
                    
                int x = i.Index % 10;
                int y = i.Index / 10;

                if (Mode == FlipType.Horizontal) x = 9 - x;
                else if (Mode == FlipType.Vertical) y = 9 - y;

                else if (Mode == FlipType.Diagonal1) {
                    int temp = x;
                    x = y;
                    y = temp;
                
                } else if (Mode == FlipType.Diagonal2) {
                    x = 9 - x;
                    y = 9 - y;

                    int temp = x;
                    x = y;
                    y = temp;
                }

                i.Index = (byte)(y * 10 + x);
                return new [] {i};
            })).ToList());
        
        public class ModeUndoEntry: EnumSimplePathUndoEntry<Flip, FlipType> {
            protected override void Action(Flip item, FlipType element) => item.Mode = element;
            
            public ModeUndoEntry(Flip flip, FlipType u, FlipType r, IEnumerable source)
            : base("Flip Orientation", flip, u, r, source) {}
            
            ModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class BypassUndoEntry: SimplePathUndoEntry<Flip, bool> {
            protected override void Action(Flip item, bool element) => item.Bypass = element;
            
            public BypassUndoEntry(Flip flip, bool u, bool r)
            : base($"Flip Bypass Changed to {(r? "Enabled" : "Disabled")}", flip, u, r) {}
            
            BypassUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}