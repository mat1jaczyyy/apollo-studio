using System;
using System.Collections.Generic;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;

namespace Apollo.Structures {
    public class Signal {
        public Launchpad Source;
        byte _index = 11;
        int _page = 1;
        public Color Color;
        public int Layer;
        public BlendingType BlendingMode;
        int _range = 200;
        public Stack<int> MultiTarget = new Stack<int>();
        public bool HashIndex = true;

        public byte Index {
            get => _index;
            set {
                if (0 <= value && value <= 100)
                    _index = value;
            }
        }

        public int Page {
            get => _page;
            set {
                if (1 <= value && value <= 100)
                    _page = value;
                else if (value == 0)
                    _page = Program.Project?.Page?? _page;
            }
        }

        public int BlendingRange {
            get => _range;
            set {
                if (1 <= value && value <= 200)
                    _range = value;
            }
        }

        public int? PeekMultiTarget => MultiTarget.TryPeek(out int target)? (int?)target : null;

        public Signal Clone() => new Signal(Source, Index, Color.Clone(), Page, Layer, BlendingMode, BlendingRange, new Stack<int>(MultiTarget.ToArray())) {
            HashIndex = HashIndex
        };

        public Signal(Launchpad source, byte index = 11, Color color = null, int page = 0, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<int> multiTarget = null) {
            Source = source;
            Index = index;
            Color = color?? new Color(63);
            Page = page;
            Layer = layer;
            BlendingMode = blending;
            BlendingRange = blendingrange;
            MultiTarget = multiTarget?? new Stack<int>();
        }

        public Signal(InputType input, Launchpad source, byte index = 11, Color color = null, int page = 0, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<int> multiTarget = null): this(
            source,
            (input == InputType.DrumRack)? Converter.DRtoXY(index) : ((index == 99)? (byte)100 : index),
            color,
            page,
            layer,
            blending,
            blendingrange,
            multiTarget
        ) {}

        public override bool Equals(object obj) {
            if (!(obj is Signal)) return false;
            return this == (Signal)obj;
        }

        public static bool operator ==(Signal a, Signal b) => a.Source == b.Source && ((a.HashIndex && b.HashIndex)? a.Index == b.Index : true) && a.Color == b.Color && a.Page == b.Page && a.Layer == b.Layer && a.BlendingMode == b.BlendingMode && a.PeekMultiTarget == b.PeekMultiTarget;
        public static bool operator !=(Signal a, Signal b) => !(a == b);
        
        public override int GetHashCode() => HashCode.Combine(Source, HashIndex? Index : 11, Color, Page, Layer, BlendingMode, BlendingRange, PeekMultiTarget);
        
        public override string ToString() => $"{((Source == null)? "null" : Source.Name )} -> {Index} @ {Layer} + {BlendingMode} & {MultiTarget} = {Color}";
    }

    public class StopSignal: Signal {
        public StopSignal(): base(null) {}
    }
}