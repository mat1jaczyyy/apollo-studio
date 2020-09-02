namespace Apollo.Enums {
    public enum Palettes {
        Monochrome, NovationPalette, CustomPalette
    }

    public enum ThemeType {
        Dark, Light
    }
    
    public enum ColorDisplayType {
        Hex, RGB
    }

    public enum LaunchpadModels {
        MK2, Pro, X, ProMK3, All
    }

    public enum LaunchpadStyles {
        Stock, Phantom, Sanded
    }

    public enum ClearType {
        Lights, Multi, Both
    }

    public enum CopyType {
        Static, Animate, Interpolate, RandomSingle, RandomLoop
    }
        
    public enum GridType {
        Full, Square
    }

    public enum FadePlaybackType {
        Mono, Loop
    }

    public enum FlipType {
        Horizontal, Vertical, Diagonal1, Diagonal2
    }
        
    public enum MultiType {
        Forward, Backward, Random, RandomPlus, Key
    }

    public enum PlaybackType {
        Mono, Poly, Loop
    }

    public enum RotateType {
        D90, D180, D270
    }

    public enum PortWarningType {
        None, Show, Done
    }

    public enum LaunchpadType {
        MK2, Pro, CFW, X, MiniMK3, ProMK3, Unknown
    }

    public enum InputType {
        XY, DrumRack
    }

    public enum RotationType {
        D0, D90, D180, D270
    }
    
    public enum BlendingType {
        Normal, Screen, Multiply, Mask
    }
    
    public enum FadeType {
        Linear, Smooth, Sharp, Fast, Slow, Hold, Release
    }

    public static class EnumExtensions {
        public static bool HasNovationLED(this LaunchpadModels model)
            => model == LaunchpadModels.X || model == LaunchpadModels.ProMK3;
        
        public static int GridSize(this LaunchpadModels model)
            => (model == LaunchpadModels.Pro || model == LaunchpadModels.ProMK3 || model == LaunchpadModels.All)
                ? 10
                : 9;
        
        public static bool HasModeLight(this LaunchpadModels model)
            => model == LaunchpadModels.Pro || model == LaunchpadModels.All;
        
        public static bool SupportsAngle(this CopyType type)
            => type == CopyType.Interpolate;
        
        public static bool SupportsRate(this CopyType type)
            => type != CopyType.Static && type != CopyType.RandomSingle;
        
        public static bool SupportsPinch(this CopyType type)
            => type == CopyType.Animate || type == CopyType.Interpolate;
        
        public static int Wrap(this GridType type, int coord)
            => (type == GridType.Square)? (coord + 7) % 8 + 1 : (coord + 10) % 10;
        
        public static bool UsesKeySelector(this MultiType type)
            => type == MultiType.Key;

        public static bool IsPro(this LaunchpadType type)
            => LaunchpadType.Pro <= type && type <= LaunchpadType.CFW;

        public static bool IsGenerationX(this LaunchpadType type)
            => LaunchpadType.X <= type && type <= LaunchpadType.ProMK3;

        public static bool HasProgrammerFwHack(this LaunchpadType type)
            => LaunchpadType.X <= type && type <= LaunchpadType.MiniMK3;

        public static bool SupportsRange(this BlendingType type)
            => type != BlendingType.Normal;

        public static FadeType Opposite(this FadeType type) {
            if (type == FadeType.Fast) return FadeType.Slow;
            if (type == FadeType.Slow) return FadeType.Fast;
            if (type == FadeType.Hold) return FadeType.Release;
            if (type == FadeType.Release) return FadeType.Hold;

            return type;
        }
    }
}