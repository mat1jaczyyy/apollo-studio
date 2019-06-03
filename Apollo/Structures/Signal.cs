using System;

using Apollo.Elements;
using Apollo.Helpers;

namespace Apollo.Structures {
    public class Signal {
        public enum BlendingType {
            Normal,
            Screen,
            Multiply,
            Mask
        }

        public Launchpad Source;
        private byte _p = 11;
        public Color Color;
        public int Layer;
        public BlendingType BlendingMode;
        public int? MultiTarget;

        public byte Index {
            get => _p;
            set {
                if (1 <= value && value <= 99)
                    _p = value;
            }
        }

        public Signal Clone() => new Signal(Source, Index, Color.Clone(), Layer, BlendingMode, MultiTarget);

        public Signal(Launchpad source, byte index = 11, Color color = null, int layer = 0, BlendingType blending = BlendingType.Normal, int? multiTarget = null) {
            Source = source;
            Index = index;
            Color = color?? new Color(63);
            Layer = layer;
            BlendingMode = blending;
            MultiTarget = multiTarget;
        }

        public Signal(Launchpad.InputType input, Launchpad source, byte index = 11, Color color = null, int layer = 0, BlendingType blending = BlendingType.Normal, int? multiTarget = null): this(
            source,
            (input == Launchpad.InputType.DrumRack)? Converter.DRtoXY(index) : index,
            color,
            layer,
            blending,
            multiTarget
        ) {}

        public override bool Equals(object obj) {
            if (!(obj is Signal)) return false;
            return this == (Signal)obj;
        }

        public static bool operator ==(Signal a, Signal b) => a.Source == b.Source && a.Index == b.Index && a.Color == b.Color && a.Layer == b.Layer && a.BlendingMode == b.BlendingMode && a.MultiTarget == b.MultiTarget;
        public static bool operator !=(Signal a, Signal b) => !(a == b);
        
        public override int GetHashCode() => HashCode.Combine(Source, Index, Color, Layer, BlendingMode, MultiTarget);

        public override string ToString() => $"{((Source == null)? "null" : Source.Name )} -> {Index} @ {Layer} + {BlendingMode} & {MultiTarget} = {Color}";
    }
}