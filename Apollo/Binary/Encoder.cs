using System;
using System.IO;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;

namespace Apollo.Binary {
    public static class Encoder {
        private static void EncodeHeader(BinaryWriter writer) {
            writer.Write(new char[] {'A', 'P', 'O', 'L'});
            writer.Write(Common.version);
        }

        private static void EncodeID(BinaryWriter writer, Type type) {
            writer.Write((byte)Array.IndexOf(Common.id, type));
        }

        public static MemoryStream Encode(object o) {
            MemoryStream output = new MemoryStream();

            using (BinaryWriter writer = new BinaryWriter(output)) {
                EncodeHeader(writer);
                Encode(writer, (dynamic)o);
            }

            return output;
        }

        public static MemoryStream EncodePreferences() {
            MemoryStream output = new MemoryStream();

            using (BinaryWriter writer = new BinaryWriter(output)) {
                EncodeHeader(writer);
                EncodeID(writer, typeof(Preferences));

                writer.Write(Preferences.AlwaysOnTop);
                writer.Write(Preferences.CenterTrackContents);
                writer.Write(Preferences.AutoCreateKeyFilter);
                writer.Write(Preferences.AutoCreatePageFilter);
                writer.Write(Preferences.FadeSmoothnessSlider);
                writer.Write(Preferences.CopyPreviousFrame);

                int count = Math.Min(64, ColorHistory.Count);
                writer.Write(count);
                for (int i = 0; i < count; i++)
                    Encode(ColorHistory.GetColor(i));
            }

            return output;
        }

        private static void Encode(BinaryWriter writer, Copyable o) {
            EncodeID(writer, typeof(Copyable));
            
            writer.Write(o.Contents.Count);
            for (int i = 0; i < o.Contents.Count; i++)
                Encode(writer, o.Contents[i]);
        }

        private static void Encode(BinaryWriter writer, Project o) {
            EncodeID(writer, typeof(Project));

            writer.Write(o.BPM);

            writer.Write(o.Page);

            writer.Write(o.Tracks.Count);
            for (int i = 0; i < o.Tracks.Count; i++)
                Encode(writer, o.Tracks[i]);
        }

        private static void Encode(BinaryWriter writer, Track o) {
            EncodeID(writer, typeof(Track));

            Encode(writer, o.Chain);
            Encode(writer, o.Launchpad);
        }

        private static void Encode(BinaryWriter writer, Chain o) {
            EncodeID(writer, typeof(Chain));

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
        }

        private static void Encode(BinaryWriter writer, Device o) {
            EncodeID(writer, typeof(Device));

            Encode(writer, (dynamic)o);
        }

        private static void Encode(BinaryWriter writer, Launchpad o) {
            EncodeID(writer, typeof(Launchpad));

            writer.Write(o.Name);
        }

        private static void Encode(BinaryWriter writer, Group o) {
            EncodeID(writer, typeof(Group));

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
            
            writer.Write(o.Expanded.HasValue);
            if (o.Expanded.HasValue)
                writer.Write(o.Expanded.Value);
        }

        private static void Encode(BinaryWriter writer, Copy o) {
            EncodeID(writer, typeof(Copy));

            writer.Write(o.Mode);
            Encode(writer, o.Length);
            writer.Write(o.Rate);
            writer.Write(o.Gate);

            writer.Write((int)o.GetCopyMode());
            writer.Write(o.Loop);

            writer.Write(o.Offsets.Count);
            for (int i = 0; i < o.Offsets.Count; i++)
                Encode(writer, o.Offsets[i]);
        }

        private static void Encode(BinaryWriter writer, Delay o) {
            EncodeID(writer, typeof(Delay));

            writer.Write(o.Mode);
            Encode(writer, o.Length);
            writer.Write(o.Time);
            writer.Write(o.Gate);
        }

        private static void Encode(BinaryWriter writer, Fade o) {
            EncodeID(writer, typeof(Fade));

            writer.Write(o.Mode);
            Encode(writer, o.Length);
            writer.Write(o.Time);
            writer.Write(o.Gate);

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o.GetColor(i));
            
            for (int i = 0; i < o.Count; i++)
                writer.Write(o.GetPosition(i));
        }

        private static void Encode(BinaryWriter writer, Flip o) {
            EncodeID(writer, typeof(Flip));

            writer.Write((int)o.GetFlipMode());
            writer.Write(o.Bypass);
        }

        private static void Encode(BinaryWriter writer, Hold o) {
            EncodeID(writer, typeof(Hold));

            writer.Write(o.Mode);
            Encode(writer, o.Length);
            writer.Write(o.Time);
            writer.Write(o.Gate);

            writer.Write(o.Infinite);
            writer.Write(o.Release);
        }

        private static void Encode(BinaryWriter writer, KeyFilter o) {
            EncodeID(writer, typeof(KeyFilter));

            for (int i = 0; i < 100; i++)
                writer.Write(o[i]);
        }

        private static void Encode(BinaryWriter writer, Layer o) {
            EncodeID(writer, typeof(Layer));

            writer.Write(o.Target);
        }

        private static void Encode(BinaryWriter writer, Move o) {
            EncodeID(writer, typeof(Move));

            Encode(writer, o.Offset);
            writer.Write(o.Loop);
        }

        private static void Encode(BinaryWriter writer, Multi o) {
            EncodeID(writer, typeof(Multi));

            Encode(writer, o.Preprocess);

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
            
            writer.Write(o.Expanded.HasValue);
            if (o.Expanded.HasValue)
                writer.Write(o.Expanded.Value);

            writer.Write((int)o.GetMultiMode());
        }

        private static void Encode(BinaryWriter writer, Output o) {
            EncodeID(writer, typeof(Output));

            writer.Write(o.Target);
        }

        private static void Encode(BinaryWriter writer, PageFilter o) {
            EncodeID(writer, typeof(PageFilter));

            for (int i = 0; i < 100; i++)
                writer.Write(o[i]);
        }

        private static void Encode(BinaryWriter writer, PageSwitch o) {
            EncodeID(writer, typeof(PageSwitch));

            writer.Write(o.Target);
        }

        private static void Encode(BinaryWriter writer, Paint o) {
            EncodeID(writer, typeof(Paint));

            Encode(writer, o.Color);
        }

        private static void Encode(BinaryWriter writer, Pattern o) {
            EncodeID(writer, typeof(Pattern));

            writer.Write(o.Gate);
            
            writer.Write(o.Frames.Count);
            for (int i = 0; i < o.Frames.Count; i++)
                Encode(writer, o.Frames[i]);
            
            writer.Write((int)o.GetPlaybackType());
            
            writer.Write(o.Choke.HasValue);
            if (o.Choke.HasValue)
                writer.Write(o.Choke.Value);

            writer.Write(o.Expanded);
        }

        private static void Encode(BinaryWriter writer, Preview o) {
            EncodeID(writer, typeof(Preview));
        }

        private static void Encode(BinaryWriter writer, Rotate o) {
            EncodeID(writer, typeof(Rotate));

            writer.Write((int)o.GetRotateMode());
            writer.Write(o.Bypass);
        }

        private static void Encode(BinaryWriter writer, Tone o) {
            EncodeID(writer, typeof(Tone));

            writer.Write(o.Hue);

            writer.Write(o.SaturationHigh);
            writer.Write(o.SaturationLow);

            writer.Write(o.ValueHigh);
            writer.Write(o.ValueLow);
        }

        private static void Encode(BinaryWriter writer, Color o) {
            EncodeID(writer, typeof(Color));

            writer.Write(o.Red);
            writer.Write(o.Green);
            writer.Write(o.Blue);
        }

        private static void Encode(BinaryWriter writer, Frame o) {
            EncodeID(writer, typeof(Frame));

            writer.Write(o.Mode);
            Encode(writer, o.Length);
            writer.Write(o.Time);

            for (int i = 0; i < 100; i++)
                Encode(writer, o.Screen[i]);
        }

        private static void Encode(BinaryWriter writer, Length o) {
            EncodeID(writer, typeof(Length));

            writer.Write(o.Step);
        }

        private static void Encode(BinaryWriter writer, Offset o) {
            EncodeID(writer, typeof(Offset));

            writer.Write(o.X);
            writer.Write(o.Y);
        }
    }
}