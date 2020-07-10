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
        public Stack<List<int>> MultiTarget = new Stack<List<int>>();

        public int Delay = 0;

        public byte Index {
            get => _index;
            set {
                if (0 <= value && value <= 100)
                    _index = value;
            }
        }
        
        public int[] Macros {
            get => _macros;
            set => _macros = value?? Program.Project?.Macros?? new int[] {1, 1, 1, 1};
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

        public List<int> PeekMultiTarget => MultiTarget.TryPeek(out List<int> target)? target : null;

        public Stack<List<int>> CopyMultiTarget() => new Stack<List<int>>(MultiTarget.ToArray().Select(i => i.ToList()).ToArray());

        public Signal Clone() => new Signal(Origin, Source, Index, Color.Clone(), (int[])Macros?.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget(), Validators.ToList()) {
            Delay = Delay
        };

        public Signal With(byte index = 11, Color color = null) => new Signal(Origin, Source, index, color, (int[])Macros.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget(), Validators.ToList()) {
            Delay = Delay
        };

        public Signal(object origin, Launchpad source, byte index = 11, Color color = null, int[] macros = null, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<List<int>> multiTarget = null, List<ValidatorDelegate> validators = null) {
            Origin = origin;
            Source = source;
            Index = index;
            Color = color?? new Color();
            Macros = macros;
            Layer = layer;
            BlendingMode = blending;
            BlendingRange = blendingrange;
            MultiTarget = multiTarget?? new Stack<List<int>>();
            Validators = validators?? new List<ValidatorDelegate>();
        }

        public Signal(InputType input, object origin, Launchpad source, byte index = 11, Color color = null, int[] macros = null, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<List<int>> multiTarget = null): this(
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

        public static bool operator ==(Signal a, Signal b) => a.Source == b.Source && a.Index == b.Index && a.Color == b.Color && a.Macros.SequenceEqual(b.Macros) && a.Layer == b.Layer && a.BlendingMode == b.BlendingMode && a.PeekMultiTarget == b.PeekMultiTarget;
        public static bool operator !=(Signal a, Signal b) => !(a == b);
        
        public override int GetHashCode() => HashCode.Combine(Source, Index, Color, HashCode.Combine(Macros[0], Macros[1], Macros[2], Macros[3]), Layer, BlendingMode, BlendingRange, GetListHashCode(PeekMultiTarget));
        
        int GetListHashCode(IEnumerable<int> x) => (x == null || x.Count() == 0)
            ? HashCode.Combine((int?)null)
            : (x.Count() > 1)
                ? HashCode.Combine(x.First(), GetListHashCode(x.Skip(1)))
                : HashCode.Combine(x.First());
        
        public override string ToString() => $"{((Source == null)? "null" : Source.Name)} -> {Index} @ {Layer} + {BlendingMode} = {Color}";
    
        public delegate bool ValidatorDelegate(out IEnumerable<Signal> extra);
        List<ValidatorDelegate> Validators;

        public void AddValidator(ValidatorDelegate validator)
            => Validators.Add(validator);

        public bool Validate(out List<Signal> extra) {
            IEnumerable<Signal> ret = Enumerable.Empty<Signal>();

            bool valid = Validators.All(i => {
                bool cur = i.Invoke(out IEnumerable<Signal> n);
                if (n != null) ret = ret.Concat(n);

                return cur;
            });

            extra = ret.Any()? ret.ToList() : null;
            return valid;
        }
    }

    //! Heaven incompatible wtf
    public class StopSignal: Signal {
        public StopSignal(): base(null, null) {}
    }
}