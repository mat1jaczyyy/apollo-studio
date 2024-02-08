using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Elements.Launchpads;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Binary {
    public static class Encoder {
        static void EncodeHeader(BinaryWriter writer) {
            writer.Write(new char[] {'A', 'P', 'O', 'L'});
            writer.Write(Common.version);
        }

        static void EncodeID(BinaryWriter writer, Type type) => writer.Write((byte)Array.IndexOf(Common.id, type));

        static byte[] Encode(Action<BinaryWriter> encoder) {
            using (MemoryStream output = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    EncodeHeader(writer);
                    encoder?.Invoke(writer);
                }

                return output.ToArray();
            }
        }

        public static byte[] EncodeConfig() => Encode(writer => {
            EncodeID(writer, typeof(Preferences));

            writer.Write(Preferences.AlwaysOnTop);
            writer.Write(Preferences.CenterTrackContents);

            writer.Write(Preferences.ChainSignalIndicators);
            writer.Write(Preferences.DeviceSignalIndicators);
            
            writer.Write((int)Preferences.ColorDisplayFormat);

            writer.Write((int)Preferences.LaunchpadStyle);
            writer.Write(Convert.ToInt32(Preferences.LaunchpadGridRotation));
            writer.Write((int)Preferences.LaunchpadModel);

            writer.Write(Preferences.AutoCreateKeyFilter);
            writer.Write(Preferences.AutoCreateMacroFilter);
            writer.Write(Preferences.AutoCreatePattern);

            writer.Write(Preferences.FPSLimit);

            writer.Write(Preferences.CopyPreviousFrame);
            writer.Write(Preferences.CaptureLaunchpad);
            writer.Write(Preferences.EnableGestures);
            writer.Write(Preferences.RememberPatternPosition);

            writer.Write(Preferences.PaletteName);

            for (int i = 0; i < 128; i++)
                Encode(writer, Preferences.CustomPalette.BackingArray[i]);

            writer.Write((int)Preferences.ImportPalette);

            writer.Write((int)Preferences.Theme);

            writer.Write(Preferences.Backup);
            writer.Write(Preferences.Autosave);

            writer.Write(Preferences.UndoLimit);
            
            writer.Write(Preferences.DiscordPresence);
            writer.Write(Preferences.DiscordFilename);

            int count;
            writer.Write(count = Math.Min(64, ColorHistory.Count));
            for (int i = 0; i < count; i++)
                Encode(writer, ColorHistory.GetColor(i));
            
            writer.Write(MIDI.Devices.Count(i => i.GetType() == typeof(Launchpad)));
            for (int i = 0; i < MIDI.Devices.Count; i++)
                if (MIDI.Devices[i].GetType() == typeof(Launchpad))
                    Encode(writer, MIDI.Devices[i]);
            
            writer.Write(Preferences.Recents.Count);
            for (int i = 0; i < Preferences.Recents.Count; i++)
                writer.Write(Preferences.Recents[i]);
            
            writer.Write(Preferences.VirtualLaunchpads.Count);
            for (int i = 0; i < Preferences.VirtualLaunchpads.Count; i++)
                writer.Write(Preferences.VirtualLaunchpads[i]);
            
            writer.Write(Preferences.Crashed);
            writer.Write(Preferences.CrashPath);
            
            writer.Write(Preferences.CheckForUpdates);
        });

        public static byte[] EncodeStats() => Encode(writer => {
            EncodeID(writer, typeof(Preferences));
            
            writer.Write(Preferences.Time);
        });
        
        public static void EncodeAnything<T>(BinaryWriter writer, dynamic o) {      
            Type type = typeof(T);          
            
            if (Nullable.GetUnderlyingType(type) != null) { // warning: will probably break if null is passed as reference type
                writer.Write(o != null);
                if (o != null)
                    writer.Write(o);
                
            } else if (type.IsEnum)
                writer.Write((int)o);

            else if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type)) { // Lists and Arrays
                Type EnumerableType = type.IsArray
                    ? type.GetElementType()
                    : type.GetGenericArguments()[0];
                
                typeof(Encoder).GetMethod("EncodeEnumerable", BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(EnumerableType)
                    .Invoke(null, new object[] { writer, o });

            } else if (typeof(Device).IsAssignableFrom(type)) 
                Encode(writer, (Device)o);
                
            else {
                MethodInfo encoder = typeof(BinaryWriter).GetMethods()
                    .Where(i => i.Name == "Write" && i.GetParameters().Count() == 1)
                    .FirstOrDefault(i => i.GetParameters().First().ParameterType == type);

                if (encoder == null) Encode(writer, o);
                else encoder.Invoke(writer, new object[] { o });
            }
        }

        static void EncodeEnumerable<T>(BinaryWriter writer, IEnumerable<T> o) {
            int count = o.Count();
            
            writer.Write(count);
            for (int i = 0; i < count; i++)
                EncodeAnything<T>(writer, o.ElementAt(i));
        }

        public static byte[] Encode(object o) => Encode(writer => {
            Encode(writer, (dynamic)o);
        });

        public static void Encode(BinaryWriter writer, Copyable o) {
            EncodeID(writer, typeof(Copyable));
            
            writer.Write(o.Contents.Count);
            for (int i = 0; i < o.Contents.Count; i++)
                if (o.Contents[i] is Device d) Encode(writer, d);
                else Encode(writer, (dynamic)o.Contents[i]);
        }

        public static void Encode(BinaryWriter writer, Project o) {
            EncodeID(writer, typeof(Project));

            writer.Write(o.BPM);
            
            for (int i = 1; i < 5; i++)
                writer.Write(o.GetMacro(i));

            writer.Write(o.Tracks.Count);
            for (int i = 0; i < o.Tracks.Count; i++)
                Encode(writer, o.Tracks[i]);

            writer.Write(o.Author);
            writer.Write(o.Time);
            writer.Write(o.Started.ToUnixTimeSeconds());

            Encode(writer, o.Undo);
        }

        public static void Encode(BinaryWriter writer, Track o) {
            EncodeID(writer, typeof(Track));

            Encode(writer, o.Chain);
            Encode(writer, o.Launchpad);
            
            writer.Write(o.Name);
            writer.Write(o.Enabled);
        }

        public static void Encode(BinaryWriter writer, Chain o) {
            EncodeID(writer, typeof(Chain));

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
            
            writer.Write(o.Name);
            writer.Write(o.Enabled);

            for (int i = 0; i < 101; i++)
                writer.Write(o.SecretMultiFilter[i]);
        }

        public static void Encode(BinaryWriter writer, Device o) {
            EncodeID(writer, typeof(Device));

            writer.Write(o.Collapsed);
            writer.Write(o.Enabled);

            Encode(writer, (dynamic)o);
        }

        public static void Encode(BinaryWriter writer, Launchpad o) {
            EncodeID(writer, typeof(Launchpad));

            if (o == MIDI.NoOutput)
                writer.Write("");
            else {
                writer.Write(o.Name);
                writer.Write((int)o.InputFormat);
                writer.Write((int)o.Rotation);
            }
        }

        public static void Encode(BinaryWriter writer, Group o) {
            EncodeID(writer, typeof(Group));

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
            
            writer.Write(o.Expanded.HasValue);
            if (o.Expanded.HasValue)
                writer.Write(o.Expanded.Value);
        }

        public static void Encode(BinaryWriter writer, Choke o) {
            EncodeID(writer, typeof(Choke));

            writer.Write(o.Target);
            Encode(writer, o.Chain);
        }

        public static void Encode(BinaryWriter writer, Clear o) {
            EncodeID(writer, typeof(Clear));

            writer.Write((int)o.Mode);
        }
        
        public static void Encode(BinaryWriter writer, ColorFilter o) {
            EncodeID(writer, typeof(ColorFilter));

            writer.Write(o.Hue);
            writer.Write(o.Saturation);
            writer.Write(o.Value);

            writer.Write(o.HueTolerance);
            writer.Write(o.SaturationTolerance);
            writer.Write(o.ValueTolerance);
        }

        public static void Encode(BinaryWriter writer, Copy o) {
            EncodeID(writer, typeof(Copy));

            Encode(writer, o.Time);
            writer.Write(o.Gate);
            writer.Write(o.Pinch);
            writer.Write(o.Bilateral);

            writer.Write(o.Reverse);
            writer.Write(o.Infinite);

            writer.Write((int)o.CopyMode);
            writer.Write((int)o.GridMode);
            writer.Write(o.Wrap);

            writer.Write(o.Offsets.Count);
            for (int i = 0; i < o.Offsets.Count; i++)
                Encode(writer, o.Offsets[i]);

            for (int i = 0; i < o.Offsets.Count; i++)
                writer.Write(o.GetAngle(i));
        }

        public static void Encode(BinaryWriter writer, Delay o) {
            EncodeID(writer, typeof(Delay));

            Encode(writer, o.Time);
            writer.Write(o.Gate);
        }

        public static void Encode(BinaryWriter writer, Fade o) {
            EncodeID(writer, typeof(Fade));

            Encode(writer, o.Time);
            writer.Write(o.Gate);
            writer.Write((int)o.PlayMode);

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o.GetColor(i));
            
            for (int i = 0; i < o.Count; i++)
                writer.Write(o.GetPosition(i));
            
            for (int i = 0; i < o.Count - 1; i++)
                writer.Write((int)o.GetFadeType(i));

            writer.Write(o.Expanded.HasValue);
            if (o.Expanded.HasValue)
                writer.Write(o.Expanded.Value);
        }

        public static void Encode(BinaryWriter writer, Flip o) {
            EncodeID(writer, typeof(Flip));

            writer.Write((int)o.Mode);
            writer.Write(o.Bypass);
        }

        public static void Encode(BinaryWriter writer, Hold o) {
            EncodeID(writer, typeof(Hold));

            Encode(writer, o.Time);
            writer.Write(o.Gate);

            writer.Write((int)o.HoldMode);
            writer.Write(o.Release);
        }

        public static void Encode(BinaryWriter writer, KeyFilter o) {
            EncodeID(writer, typeof(KeyFilter));

            for (int i = 0; i < 101; i++)
                writer.Write(o[i]);
        }

        public static void Encode(BinaryWriter writer, Layer o) {
            EncodeID(writer, typeof(Layer));

            writer.Write(o.Target);
            writer.Write((int)o.BlendingMode);
            writer.Write(o.Range);
        }

        public static void Encode(BinaryWriter writer, LayerFilter o) {
            EncodeID(writer, typeof(LayerFilter));

            writer.Write(o.Target);
            writer.Write(o.Range);
        }
        
        public static void Encode(BinaryWriter writer, Loop o){
            EncodeID(writer, typeof(Loop));
            
            Encode(writer, o.Rate);
            writer.Write(o.Gate);

            writer.Write(o.Repeats);
            writer.Write(o.Hold);
        }

        public static void Encode(BinaryWriter writer, Move o) {
            EncodeID(writer, typeof(Move));

            Encode(writer, o.Offset);
            writer.Write((int)o.GridMode);
            writer.Write(o.Wrap);
        }

        public static void Encode(BinaryWriter writer, Multi o) {
            EncodeID(writer, typeof(Multi));

            Encode(writer, o.Preprocess);

            writer.Write(o.Count);
            for (int i = 0; i < o.Count; i++)
                Encode(writer, o[i]);
            
            writer.Write(o.Expanded.HasValue);
            if (o.Expanded.HasValue)
                writer.Write(o.Expanded.Value);

            writer.Write((int)o.Mode);
        }

        public static void Encode(BinaryWriter writer, Output o) {
            EncodeID(writer, typeof(Output));

            writer.Write(o.Target);
        }

        public static void Encode(BinaryWriter writer, MacroFilter o) {
            EncodeID(writer, typeof(MacroFilter));

            writer.Write(o.Macro);
            
            for (int i = 0; i < 100; i++)
                writer.Write(o[i]);
        }

        public static void Encode(BinaryWriter writer, Switch o) {
            EncodeID(writer, typeof(Switch));

            writer.Write(o.Target);
            writer.Write(o.Value);
        }

        public static void Encode(BinaryWriter writer, Paint o) {
            EncodeID(writer, typeof(Paint));

            Encode(writer, o.Color);
        }

        public static void Encode(BinaryWriter writer, Pattern o) {
            EncodeID(writer, typeof(Pattern));

            writer.Write(o.Repeats);
            writer.Write(o.Gate);

            writer.Write(o.Pinch);
            writer.Write(o.Bilateral);
            
            writer.Write(o.Frames.Count);
            for (int i = 0; i < o.Frames.Count; i++)
                Encode(writer, o.Frames[i]);
            
            writer.Write((int)o.Mode);
            writer.Write(o.Infinite);

            writer.Write(o.RootKey.HasValue);
            if (o.RootKey.HasValue)
                writer.Write(o.RootKey.Value);

            writer.Write(o.Wrap);

            writer.Write(o.Expanded);
        }

        public static void Encode(BinaryWriter writer, Preview o) {
            EncodeID(writer, typeof(Preview));
        }

        public static void Encode(BinaryWriter writer, Rotate o) {
            EncodeID(writer, typeof(Rotate));

            writer.Write((int)o.Mode);
            writer.Write(o.Bypass);
        }

        public static void Encode(BinaryWriter writer, Refresh o) {
            EncodeID(writer, typeof(Refresh));

            for (int i = 0; i < 4; i++)
                writer.Write(o.GetMacro(i));
        }

        public static void Encode(BinaryWriter writer, Tone o) {
            EncodeID(writer, typeof(Tone));

            writer.Write(o.Hue);

            writer.Write(o.SaturationHigh);
            writer.Write(o.SaturationLow);

            writer.Write(o.ValueHigh);
            writer.Write(o.ValueLow);
        }

        public static void Encode(BinaryWriter writer, Color o) {
            EncodeID(writer, typeof(Color));

            writer.Write(o.Red);
            writer.Write(o.Green);
            writer.Write(o.Blue);
        }

        public static void Encode(BinaryWriter writer, Frame o) {
            EncodeID(writer, typeof(Frame));

            Encode(writer, o.Time);

            for (int i = 0; i < 101; i++)
                Encode(writer, o.Screen[i]);
        }

        public static void Encode(BinaryWriter writer, Length o) {
            EncodeID(writer, typeof(Length));

            writer.Write(o.Step);
        }

        public static void Encode(BinaryWriter writer, Offset o) {
            EncodeID(writer, typeof(Offset));

            writer.Write(o.X);
            writer.Write(o.Y);

            writer.Write(o.IsAbsolute);
            writer.Write(o.AbsoluteX);
            writer.Write(o.AbsoluteY);
        }

        public static void Encode(BinaryWriter writer, Time o) {
            EncodeID(writer, typeof(Time));

            writer.Write(o.Mode);
            Encode(writer, o.Length);
            writer.Write(o.Free);
        }

        public static void Encode(BinaryWriter writer, UndoManager o) {
            EncodeID(writer, typeof(UndoManager));

            writer.Write(UndoBinary.Version);

            using (MemoryStream undoData = new MemoryStream())
                using (BinaryWriter undoWriter = new BinaryWriter(undoData)) {
                    undoWriter.Write(o.History.Count);

                    for (int i = 0; i < o.History.Count; i++) {
                        o.History[i].Encode(undoWriter);
                    }

                    undoWriter.Write(o.Position);

                    writer.Write((int)undoData.Length);
                    writer.Write(undoData.ToArray());
                }
        }
    }
}