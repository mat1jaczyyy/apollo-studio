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
        MK2, Pro, X, All
    }

    public enum LaunchpadStyles {
        Stock, Phantom, Sanded
    }

    public enum ClearType {
        Lights,
        Multi,
        Both
    }

    public enum CopyType {
        Static,
        Animate,
        Interpolate,
        RandomSingle,
        RandomLoop
    }
        
    public enum GridType {
        Full,
        Square
    }

    public enum FadePlaybackType {
        Mono,
        Loop
    }

    public enum FlipType {
        Horizontal,
        Vertical,
        Diagonal1,
        Diagonal2
    }
        
    public enum MultiType {
        Forward,
        Backward,
        Random,
        RandomPlus,
        Key
    }

    public enum PlaybackType {
        Mono,
        Poly,
        Loop
    }

    public enum RotateType {
        D90,
        D180,
        D270
    }

    public enum PortWarningType {
        None, Show, Done
    }

    public enum LaunchpadType {
        MK2, PRO, CFW, X, MiniMK3, Unknown
    }

    public enum InputType {
        XY, DrumRack
    }

    public enum RotationType {
        D0,
        D90,
        D180,
        D270
    }
    
    public enum BlendingType {
        Normal,
        Screen,
        Multiply,
        Mask
    }
    
    public enum FadeType {
        Linear,
        Smooth,
        Sharp,
        Fast,
        Slow,
        Hold
    }
}