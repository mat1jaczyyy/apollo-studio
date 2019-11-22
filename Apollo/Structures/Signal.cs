using System;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;

namespace Apollo.Structures {
    public class Signal {
        public object Origin;
        public Launchpad Source;
        byte _index = 11;
        int[] _macros;
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
        
        public int[] Macros {
            get => _macros;
            set => _macros = value?? Program.Project?.Macros?? _macros;
        }

        public int GetMacro(int target) => Macros[target - 1];
        
        public void SetMacro(int target, int value) => Macros[target] = value;

        public int BlendingRange {
            get => _range;
            set {
                if (1 <= value && value <= 200)
                    _range = value;
            }
        }

        public int? PeekMultiTarget => MultiTarget.TryPeek(out int target)? (int?)target : null;

        public Stack<int> CopyMultiTarget() => new Stack<int>(MultiTarget.ToArray());

        public Signal Clone() => new Signal(Origin, Source, Index, Color.Clone(), (int[])Macros.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget()) {
            HashIndex = HashIndex
        };

        public Signal With(byte index = 11, Color color = null) => new Signal(Origin, Source, index, color, (int[])Macros.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget());

        public Signal(object origin, Launchpad source, byte index = 11, Color color = null, int[] macros = null, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<int> multiTarget = null) {
            Origin = origin;
            Source = source;
            Index = index;
            Color = color?? new Color();
            Macros = macros;
            Layer = layer;
            BlendingMode = blending;
            BlendingRange = blendingrange;
            MultiTarget = multiTarget?? new Stack<int>();
        }

        public Signal(InputType input, object origin, Launchpad source, byte index = 11, Color color = null, int[] macros = null, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<int> multiTarget = null): this(
            origin,
            source,
            (input == InputType.DrumRack)? Converter.DRtoXY(index) : ((index == 99)? (byte)100 : index),
            color,
            macros,
            layer,
            blending,
            blendingrange,
            multiTarget
        ) {}

        public override bool Equals(object obj) {
            if (!(obj is Signal)) return false;
            return this == (Signal)obj;
        }

        public static bool operator ==(Signal a, Signal b) => a.Source == b.Source && ((a.HashIndex && b.HashIndex)? a.Index == b.Index : true) && a.Color == b.Color && a.Macros.SequenceEqual(b.Macros) && a.Layer == b.Layer && a.BlendingMode == b.BlendingMode && a.PeekMultiTarget == b.PeekMultiTarget;
        public static bool operator !=(Signal a, Signal b) => !(a == b);
        
        public override int GetHashCode() => HashCode.Combine(Source, HashIndex? Index : 11, Color, HashCode.Combine(Macros[0], Macros[1], Macros[2], Macros[3]), Layer, BlendingMode, BlendingRange, PeekMultiTarget);
        
        public override string ToString() => $"{((Source == null)? "null" : Source.Name)} -> {Index} @ {Layer} + {BlendingMode} & {MultiTarget} = {Color}";
    }

    public class StopSignal: Signal {
        public StopSignal(): base(null, null) {}
    }
}