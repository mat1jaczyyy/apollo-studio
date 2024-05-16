using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Rendering;
using Apollo.RtMidi;
using Apollo.RtMidi.Devices;
using Apollo.RtMidi.Devices.Infos;
using Apollo.Structures;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.Elements.Launchpads {
    public class Launchpad {
        public static PortWarning MK2FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad MK2s are running an older version of the\n" + 
            "official Novation firmware which is not compatible with Apollo Studio.\n\n" +
            "Update these to the faster, modded firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning MK2FirmwareStock { get; private set; } = new PortWarning(
            "One or more connected Launchpad MK2s are running\n" + 
            "the official Novation firmware.\n" + 
            "While they will work with Apollo Studio, the official firmware\n" +
            "performs slightly worse than the faster, modded stock firmware.\n\n" +
            "Update these to the faster, modded firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning ProFirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running an older version of the\n" + 
            "official Novation firmware. While they will work with Apollo Studio, this\n" +
            "version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning ProFirmwareStock { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running\n" + 
            "the official Novation firmware.\n" + 
            "While they will work with Apollo Studio, the official firmware\n" +
            "performs considerably worse than the optimized custom firmware.\n\n" +
            "Update these to the latest version of the custom firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning CFWIncompatible { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running an older version of the\n" + 
            "performance-optimized custom firmware which is not compatible with\n" +
            "Apollo Studio.\n\n" +
            "Update these to the latest version of the custom firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning XFirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Xs are running an older version of the\n" + 
            "official Novation firmware. While they will work with Apollo Studio,\n" +
            "this version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the\n" +
            "Launchpad Firmware Utility (or Novation Components) to avoid\n" +
            "any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-x/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning XFirmwareStock { get; private set; } = new PortWarning(
            "One or more connected Launchpad Xs are running\n" + 
            "the official Novation firmware.\n" + 
            "While they will work with Apollo Studio, the official firmware\n" +
            "performs slightly worse than the faster, modded stock firmware.\n\n" +
            "Update these to the faster, modded firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning XFirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Launchpad Xs are running a version of\n" + 
            "the official Novation firmware which is not compatible with \n" +
            "Apollo Studio due to having a broken Legacy mode.\n\n" +
            "Update these to the latest working version of the firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning MiniMK3FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Mini MK3s are running an older version of\n" + 
            "the official Novation firmware. While they will work with Apollo Studio,\n" +
            "this version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the\n" +
            "Launchpad Firmware Utility (or Novation Components) to avoid\n" +
            "any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-mini-mk3/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning MiniMK3FirmwareStock { get; private set; } = new PortWarning(
            "One or more connected Launchpad Mini MK3s are running\n" + 
            "the official Novation firmware.\n" + 
            "While they will work with Apollo Studio, the official firmware\n" +
            "performs slightly worse than the faster, modded stock firmware.\n\n" +
            "Update these to the faster, modded firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning MiniMK3FirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Launchpad Mini MK3s are running a version of\n" + 
            "the official Novation firmware which is not compatible with \n" +
            "Apollo Studio due to having a broken Legacy mode.\n\n" +
            "Update these to the latest working version of the firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning ProMK3FirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pro MK3s are running an older version of\n" + 
            "the official Novation firmware which is not compatible with \n" +
            "Apollo Studio due to not having a dedicated Legacy mode.\n\n" +
            "Update these to the latest version of the firmware using the\n" +
            "Launchpad Firmware Utility (or Novation Components) to avoid\n" +
            "any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-pro-mk3/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning MatrixFEFirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Founder Edition Matrixes are running an\n" + 
            "older version of the official firmware which is not compatible with\n" +
            "Apollo Studio due lack of support.\n\n" +
            "Update these to the latest version of the firmware.",
            new PortWarning.Option(
                "203Electronics MatrixOS Releases",
                "https://github.com/203Electronics/MatrixOS/releases"
            )
        );

        public static PortWarning MatrixFirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Matrixes are running an older version of\n" + 
            "the official firmware which is not compatible with \n" +
            "Apollo Studio due lack of support.\n\n" +
            "Update these to the latest version of the firmware.",
            new PortWarning.Option(
                "203Electronics MatrixOS Releases",
                "https://github.com/203Electronics/MatrixOS/releases"
            )
        );

        public static PortWarning MatrixProFirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Matrix Pros are running an older version of\n" + 
            "the official firmware which is not compatible with \n" +
            "Apollo Studio due lack of support.\n\n" +
            "Update these to the latest version of the firmware.",
            new PortWarning.Option(
                "203Electronics MatrixOS Releases",
                "https://github.com/203Electronics/MatrixOS/releases"
            )
        );

        public static PortWarning MF64FirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Midi Fighter 64s are running the official\n" + 
            "firmware which is not compatible with Apollo Studio.\n\n" +
            "Update these to the latest version of the custom firmware using the\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static void DisplayWarnings(Window sender) {
            Dispatcher.UIThread.Post(() => {
                if (MK2FirmwareOld.DisplayWarning(sender)) return;
                if (MK2FirmwareStock.DisplayWarning(sender)) return;
                if (ProFirmwareOld.DisplayWarning(sender)) return;
                if (ProFirmwareStock.DisplayWarning(sender)) return;
                if (CFWIncompatible.DisplayWarning(sender)) return;
                if (XFirmwareOld.DisplayWarning(sender)) return;
                if (XFirmwareStock.DisplayWarning(sender)) return;
                if (XFirmwareUnsupported.DisplayWarning(sender)) return;
                if (MiniMK3FirmwareOld.DisplayWarning(sender)) return;
                if (MiniMK3FirmwareStock.DisplayWarning(sender)) return;
                if (MiniMK3FirmwareUnsupported.DisplayWarning(sender)) return;
                if (ProMK3FirmwareUnsupported.DisplayWarning(sender)) return;
                if (MatrixFEFirmwareUnsupported.DisplayWarning(sender)) return;
                if (MatrixFirmwareUnsupported.DisplayWarning(sender)) return;
                if (MatrixProFirmwareUnsupported.DisplayWarning(sender)) return;
                if (MF64FirmwareUnsupported.DisplayWarning(sender)) return;
            }, DispatcherPriority.MinValue);
        }

        public LaunchpadWindow Window;
        public LaunchpadInfo Info;

        PatternWindow _patternwindow;
        public virtual PatternWindow PatternWindow {
            get => _patternwindow;
            set => _patternwindow = value;
        }

        public List<AbletonLaunchpad> AbletonLaunchpads = new();

        IMidiInputDevice Input;
        IMidiOutputDevice Output;

        public LaunchpadType Type { get; protected set; } = LaunchpadType.Unknown;

        static Dictionary<LaunchpadType, int> MaxRepeats = new() {
            {LaunchpadType.Pro, 78},
            {LaunchpadType.CFW, 79}
        };

        static byte[] SysExStart = new byte[] { 0xF0 };
        static byte[] SysExEnd = new byte[] { 0xF7 };
        static byte[] NovationHeader = new byte[] {0x00, 0x20, 0x29, 0x02};
        static byte[] MatrixHeader = new byte[] {0x00, 0x02, 0x03, 0x4D, 0x58};

        static Dictionary<LaunchpadType, byte[]> RGBHeader = new() {
            {LaunchpadType.MK2, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x18, 0x0B}).ToArray()},
            {LaunchpadType.Pro, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0B}).ToArray()},
            {LaunchpadType.CFW, SysExStart.Concat(new byte[] {0x6F}).ToArray()},
            {LaunchpadType.X, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0C, 0x03}).ToArray()},
            {LaunchpadType.MiniMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0D, 0x03}).ToArray()},
            {LaunchpadType.ProMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0E, 0x03}).ToArray()},
            {LaunchpadType.MatrixFE, SysExStart.Concat(MatrixHeader).Concat(new byte[] {0x5E}).ToArray()},
            {LaunchpadType.Matrix, SysExStart.Concat(MatrixHeader).Concat(new byte[] {0x5E}).ToArray()},
            {LaunchpadType.MatrixPro, SysExStart.Concat(MatrixHeader).Concat(new byte[] {0x5E}).ToArray()},
            {LaunchpadType.MF64, SysExStart.Concat(new byte[] {0x6F}).ToArray()}
        };

        static byte[] ProGridMessage = SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0F, 0x00}).ToArray();

        static Dictionary<LaunchpadType, byte[]> ForceClearMessage = new() {
            {LaunchpadType.MK2, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x18, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.Pro, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.CFW, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.X, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0C, 0x02, 0x00}).ToArray()},
            {LaunchpadType.MiniMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0D, 0x02, 0x00}).ToArray()},
            {LaunchpadType.ProMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0E, 0x03}).Concat(
                Enumerable.Range(0, 109).SelectMany(i => new byte[] {0x00, (byte)i, 0x00})
            ).ToArray()},
            {LaunchpadType.MatrixFE, SysExStart.Concat(MatrixHeader).Concat(new byte[] {0x5F, 0x00, 0x00, 0x40, 0x00}).ToArray()},
            {LaunchpadType.Matrix, SysExStart.Concat(MatrixHeader).Concat(new byte[] {0x5F, 0x00, 0x00, 0x40, 0x00}).ToArray()},
            {LaunchpadType.MatrixPro, SysExStart.Concat(MatrixHeader).Concat(new byte[] {0x5F, 0x00, 0x00, 0x40, 0x00}).ToArray()},
            {LaunchpadType.MF64, SysExStart.Concat(new byte[] {0x6E}).ToArray()}
        };

        static Dictionary<byte, LaunchpadType> MatrixDevices = new() {
            {0x00, LaunchpadType.MatrixFE},  // Matrix Founder Edition (Matrix Block 5 - Standard)
            {0x10, LaunchpadType.Matrix},    // Matrix                 (Matrix Block 6 - Standard)
            {0x11, LaunchpadType.MatrixPro}  // Matrix Pro             (Matrix Block 6 - Pro)
        };

        static Dictionary<LaunchpadType, PortWarning> MatrixPortWarnings = new() {
            {LaunchpadType.MatrixFE, MatrixFEFirmwareUnsupported},
            {LaunchpadType.Matrix, MatrixFirmwareUnsupported},
            {LaunchpadType.MatrixPro, MatrixProFirmwareUnsupported}
        };
        
        InputType _format = InputType.DrumRack;
        public InputType InputFormat {
            get => _format;
            set {
                if (_format != value) {
                    _format = value;
                    
                    Preferences.Save();
                }
            }
        }

        RotationType _rotation = RotationType.D0;
        public virtual RotationType Rotation {
            get => _rotation;
            set {
                if (_rotation != value) {
                    _rotation = value;
                    
                    Preferences.Save();
                }
            }
        }

        public string Name { get; protected set; }
        public bool Available { get; protected set; }

        public bool Usable => Available && Type != LaunchpadType.Unknown;

        bool SupportsCompression;

        public delegate void ReceiveEventHandler(Signal n);
        public event ReceiveEventHandler Receive;

        protected void InvokeReceive(Signal n) => Receive?.Invoke(n);

        protected Screen screen = new Screen();
        ConcurrentQueue<byte[]> buffer;
        object locker;
        int[][] inputbuffer;
        ulong signalCount = 0;

        protected void CreateScreen() {
            buffer = new ConcurrentQueue<byte[]>();
            locker = new object();
            inputbuffer = Enumerable.Range(0, 101).Select(i => (int[])null).ToArray();

            screen.ScreenExit = Send;
            screen.Clear();
        }

        public Color GetColor(int index) => (PatternWindow == null)
            ? screen.GetColor(index)
            : PatternWindow.PatternDevice.Frames[PatternWindow.PatternDevice.Expanded].Screen[index].Clone();

        static readonly byte[] DeviceInquiry = new byte[] {0xF0, 0x7E, 0x7F, 0x06, 0x01, 0xF7};
        static readonly byte[] VersionInquiry = new byte[] {0xF0, 0x00, 0x20, 0x29, 0x00, 0x70, 0xF7};

        bool doingMK2VersionInquiry = false;

        LaunchpadType AttemptIdentify(MidiMessage response) {
            if (doingMK2VersionInquiry) {
                doingMK2VersionInquiry = false;

                if (response.Data.Length != 19)
                    return LaunchpadType.Unknown;
                
                if (response.CheckSysExHeader(new byte[] {0x00, 0x20, 0x29, 0x00}) && response.Data[5] == 0x70) {
                    int versionInt = int.Parse(string.Join("", response.Data.SkipLast(3).TakeLast(3)));

                    if (versionInt < 171) // Old Firmware
                        MK2FirmwareOld.Set();
                    
                    return LaunchpadType.Unknown; // Bootloader
                }
                
                return LaunchpadType.Unknown;
            }

            if (response.Data.Length != 17)
                return LaunchpadType.Unknown;

            if (response.Data[1] != 0x7E || response.Data[3] != 0x06 || response.Data[4] != 0x02)
                return LaunchpadType.Unknown;

            // Manufacturer = Novation
            if (response.Data[5] == 0x00 && response.Data[6] == 0x20 && response.Data[7] == 0x29) {
                IEnumerable<byte> version = response.Data.SkipLast(1).TakeLast(3);
                string versionStr = string.Join("", version.Select(i => (char)i));
                int versionInt = int.Parse(string.Join("", version));

                switch (response.Data[8]) {
                    case 0x69: // Launchpad MK2
                        if (versionInt < 171) { // Old Firmware or Bootloader?
                            doingMK2VersionInquiry = true;
                            return LaunchpadType.Unknown;
                        }
                        
                        if (versionInt == 172)
                            SupportsCompression = true;

                        else MK2FirmwareStock.Set();

                        return LaunchpadType.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (versionStr == "\0\0\0") // Bootloader
                            return LaunchpadType.Unknown;

                        if (versionStr == "cfw" || versionStr == "cfx") { // Old Custom Firmware
                            CFWIncompatible.Set();
                            return LaunchpadType.Unknown;
                        }

                        if (versionStr == "cfy") { // Custom Firmware
                            SupportsCompression = true;
                            return LaunchpadType.CFW;
                        }
                        
                        if (versionInt < 182) // Old Firmware
                            ProFirmwareOld.Set();

                        ProFirmwareStock.Set();
                        return LaunchpadType.Pro;

                    case 0x03: // Launchpad X
                        if (response.Data[9] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 351) // Old Firmware
                            XFirmwareOld.Set();
                        
                        if (versionInt == 408) { // Broken Legacy mode
                            XFirmwareUnsupported.Set();
                            return LaunchpadType.Unknown;
                        }
                        
                        if (versionInt == 352)
                            SupportsCompression = true;

                        else XFirmwareStock.Set();

                        return LaunchpadType.X;
                    
                    case 0x13: // Launchpad Mini MK3
                        if (response.Data[9] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 407) // Old Firmware
                            MiniMK3FirmwareOld.Set();
                        
                        if (versionInt == 454) { // Broken Legacy mode
                            MiniMK3FirmwareUnsupported.Set();
                            return LaunchpadType.Unknown;
                        }

                        if (versionInt == 408)
                            SupportsCompression = true;

                        else MiniMK3FirmwareStock.Set();

                        return LaunchpadType.MiniMK3;
                    
                    case 0x23: // Launchpad Pro MK3
                        if (response.Data[9] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 465) { // No Legacy mode
                            ProMK3FirmwareUnsupported.Set();
                            return LaunchpadType.Unknown;
                        }

                        return LaunchpadType.ProMK3;
                } 

            // Manufacturer = 203 Electronics, Family = Matrix
            } else if (response.Data[5] == 0x00 && response.Data[6] == 0x02 && response.Data[7] == 0x03 && response.Data[8] == 0x4D && response.Data[9] == 0x58) {
                // Only Matrix OS devices are supported, so there is a common data structure
                // 12 => Major version
                // 13 => Minor version
                // 14 => Patch version (ignored if build mode isn't stable)
                // 15 => Build mode
                int versionInt = (response.Data[12] << 16) | (response.Data[13] << 8) | response.Data[14];

                LaunchpadType type = MatrixDevices.GetValueOrDefault(response.Data[10], LaunchpadType.Unknown);
                
                if (type == LaunchpadType.Unknown)
                    return LaunchpadType.Unknown;

                if (versionInt < 0x020401) { // Old Firmware
                    MatrixPortWarnings[type].Set();
                    return LaunchpadType.Unknown;
                }

                SupportsCompression = true;
                return type;
            
            // Manufacturer = DJTechTools
            } else if (response.Data[5] == 0x00 && response.Data[6] == 0x01 && response.Data[7] == 0x79 ) {
                ushort family = (ushort)(response.Data[9] << 8 | response.Data[8]);
                ushort model = (ushort)(response.Data[11] << 8 | response.Data[10]);
                uint version = (ushort)(response.Data[12] << 24 | response.Data[13] << 16 | response.Data[14] << 8 | response.Data[15]);

                if (family == 0x0006 && model == 0x0001) { // Midi Fighter 64
                    if (response.Data[12] != 0x30) { // Not CFW
                        MF64FirmwareUnsupported.Set();
                        return LaunchpadType.Unknown;
                    }

                    SupportsCompression = true;
                    return LaunchpadType.MF64;
                }

                return LaunchpadType.Unknown;
            }

            return LaunchpadType.Unknown;
        }

        void WaitForIdentification(MidiMessage e) {
            if (!e.IsSysEx) return;

            if ((Type = AttemptIdentify(e)) != LaunchpadType.Unknown) {
                Input.Received -= WaitForIdentification;

                ForceClear();

                Input.Received += HandleMidi;

                MIDI.DoneIdentifying();

            } else {
                Task.Delay(1500).ContinueWith(_ => {
                    if (Available && Type == LaunchpadType.Unknown)
                        Output.Send(doingMK2VersionInquiry? VersionInquiry : DeviceInquiry);
                });
            }
        }

        void SysExSend(IEnumerable<byte> raw) {
            if (!Usable || raw is null) return;

            buffer.Enqueue(raw.Concat(SysExEnd).ToArray());
            ulong current = signalCount;

            Task.Run(() => {
                lock (locker) {
                    if (buffer.TryDequeue(out byte[] msg)) {
                        //Console.WriteLine($"SysEx OUT {string.Join(", ", msg.Select(i => i.ToString()))}");
                        Output.Send(msg);
                    }
                    
                    signalCount++;
                }
            });

            // This protects from deadlock for some reason
            Task.Delay(1000).ContinueWith(_ => {
                if (signalCount <= current)
                    Disconnect(false);
            });
        }

        public void DirectSend(RawUpdate n) => RGBSend(new List<RawUpdate>() {n});
        
        public virtual void Send(List<RawUpdate> n, Color[] snapshot) {
            if (!n.Any() || !Usable) return;

            if (Window != null || AbletonLaunchpads.Any(i => i.Window != null)) {
                List<RawUpdate> c = n.Select(i => i.Clone()).ToList();

                Dispatcher.UIThread.InvokeAsync(() => c.ForEach(i => {
                    Window?.Render(i);

                    foreach (AbletonLaunchpad alp in AbletonLaunchpads)
                        alp.Window?.Render(i);
                }));
            }

            foreach (RawUpdate i in n.Where(i => i.Index != 100)) {
                if (Rotation == RotationType.D90) i.Index = (byte)((i.Index % 10) * 10 + 9 - i.Index / 10);
                else if (Rotation == RotationType.D180) i.Index = (byte)((9 - i.Index / 10) * 10 + 9 - i.Index % 10);
                else if (Rotation == RotationType.D270) i.Index = (byte)((9 - i.Index % 10) * 10 + i.Index / 10);
            }

            if (Optimize(n, out IEnumerable<byte> sysex)) {
                SysExSend(sysex);
                return;
            }

            List<RawUpdate> output = n.SelectMany(i => {              
                IEnumerable<RawUpdate> ret = Enumerable.Empty<RawUpdate>();

                switch (Type) {
                    case LaunchpadType.MK2:
                        if (i.Index % 10 == 0 || i.Index < 11 || i.Index == 99 || i.Index == 100) return ret;
                        if (91 <= i.Index && i.Index <= 98) i.Offset(13);
                        break;
                    
                    case LaunchpadType.Pro:
                    case LaunchpadType.CFW:
                        if (i.Index == 0 || i.Index == 9 || i.Index == 90 || i.Index == 99) return ret;
                        else if (i.Index == 100) i.Offset(-1);
                        break;

                    case LaunchpadType.X:
                    case LaunchpadType.MiniMK3:
                        if (i.Index % 10 == 0 || i.Index < 11 || i.Index == 100) return ret;
                        break;
                        
                    case LaunchpadType.ProMK3:
                        if (i.Index == 0 || i.Index == 9 || i.Index == 100) return ret;
                        if (1 <= i.Index && i.Index <= 8) ret = ret.Append(new RawUpdate(i, 100));
                        break;
                
                    case LaunchpadType.MatrixFE:
                    case LaunchpadType.Matrix:
                        if (i.Index % 10 == 0 || i.Index % 10 == 9 || i.Index < 11 || i.Index > 88 || i.Index == 100) return ret;
                        break;

                    case LaunchpadType.MatrixPro:
                        if (i.Index == 0 || i.Index == 9 || i.Index == 90 || i.Index == 99 || i.Index == 100) return ret;
                        break;

                    case LaunchpadType.MF64:
                        if (i.Index % 10 == 0 || i.Index % 10 == 9 || i.Index < 11 || i.Index > 88 || i.Index == 100) return ret;
                        i.Index = Converter.XYtoMF64(i.Index);
                        break;
                }
                
                return ret.Append(i).ToArray();

            }).ToList();

            // https://customer.novationmusic.com/sites/customer/files/novation/downloads/10598/launchpad-pro-programmers-reference-guide_0.pdf
            // Page 22: The <LED> <Red> <Green> <Blue> group may be repeated in the message up to 78 times.
            if (Type.IsPro() && output.Count > MaxRepeats[Type]) {
                if (snapshot != null) {
                    SysExSend(ProGridMessage.Concat(Enumerable.Range(0, 100)
                        .Select(i => snapshot[i])
                        .SelectMany(i => new byte[] { i.Red, i.Green, i.Blue })
                        .Concat(SysExEnd)
                    ));
                    
                    RawUpdate mode = output.FirstOrDefault(i => i.Index == 99);
                    if (mode != null) RGBSend(new List<RawUpdate>() { mode });

                } else 
                    for (int i = 0; i < output.Count; i += MaxRepeats[Type])
                        RGBSend(output.Skip(i).Take(MaxRepeats[Type]).ToList());
                
                return;
            }
            
            RGBSend(output);
        }

        static IEnumerable<byte> allMap = Enumerable.Range(1, 98).Except(new int[] {9, 90}).Select(i => (byte)i);
        static IEnumerable<byte> rowMap(int index) => Enumerable.Range(1, 8).Select(i => (byte)(index * 10 + i));
        static IEnumerable<byte> colMap(int index) => Enumerable.Range(0, 8).Select(i => (byte)(index + 10 + i * 10));

        static IEnumerable<byte> cols = Enumerable.Range(111, 8).Select(i => (byte)i);
        static IEnumerable<byte> squares = Enumerable.Range(101, 8).Select(i => (byte)i).Concat(cols);
        static IEnumerable<byte> mf64cols = Enumerable.Range(104, 8).Select(i => (byte)i);
        static IEnumerable<byte> mf64squares = Enumerable.Range(96, 8).Select(i => (byte)i).Concat(mf64cols);

        // TODO for later: this can be implemented a lot better
        static Dictionary<LaunchpadType, HashSet<byte>> excludedIndexes = new() {
            {LaunchpadType.MK2, new HashSet<byte>() {100, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 30, 40, 50, 60, 70, 80, 90, 99}},
            {LaunchpadType.CFW, new HashSet<byte>() {0, 9, 90, 99}},
            {LaunchpadType.X, new HashSet<byte>() {100, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 30, 40, 50, 60, 70, 80, 90}},
            {LaunchpadType.MiniMK3, new HashSet<byte>() {100, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 30, 40, 50, 60, 70, 80, 90}},
            {LaunchpadType.MatrixFE, new HashSet<byte>() {100, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 19, 20, 29, 30, 39, 40, 49, 50, 59, 60, 69, 70, 79, 80, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99}},
            {LaunchpadType.Matrix, new HashSet<byte>() {100, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 19, 20, 29, 30, 39, 40, 49, 50, 59, 60, 69, 70, 79, 80, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99}},
            {LaunchpadType.MatrixPro, new HashSet<byte>() {100, 0, 9, 90, 99}},
            {LaunchpadType.MF64, new HashSet<byte>() {100, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 19, 20, 29, 30, 39, 40, 49, 50, 59, 60, 69, 70, 79, 80, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99}},
        };

        bool Optimize(List<RawUpdate> updates, out IEnumerable<byte> ret) {
            ret = null;
            
            if (!SupportsCompression) return false;

            List<RawUpdate> filteredUpdates = updates.Where(u => !excludedIndexes[Type].Contains(u.Index)).ToList();
            if (!filteredUpdates.Any()) return true;

            IEnumerable<Color> colors = filteredUpdates.Select(i => i.Color).Distinct();
            if (Type.IsPro() && colors.Count() > 79) return false; // CFW Hard Limit

            ret = SysExStart;

            if (Type.IsMatrix())
                ret = ret.Concat(MatrixHeader);

            ret = ret
                .Concat(new byte[] { 0x5F })
                .Concat(
                    colors.SelectMany(i => {
                        IEnumerable<byte> positions = filteredUpdates.Where(j => j.Color == i).Select(j => j.Index);
                        if (Type.IsPro()) positions = positions.Select(j => j == 100? (byte)99 : j);

                        IEnumerable<byte> finalPos = Enumerable.Empty<byte>();
                        List<byte> chunk = new() { i.Red, i.Green, i.Blue };

                        if (Type == LaunchpadType.MF64) {
                            finalPos = positions.ToList();

                            for (int j = 1; j < 9; j++) {
                                IEnumerable<byte> row = rowMap(j);
                                IEnumerable<byte> col = colMap(j);

                                if (positions.Intersect(row).Count() == 8)
                                    finalPos = finalPos.Except(row).Append((byte)(96 + j - 1));
                                    
                                if (positions.Intersect(col).Count() == 8)
                                    finalPos = finalPos.Except(col).Append((byte)(104 + j - 1));
                            }

                            if (finalPos.Intersect(mf64squares).Count() == 16)
                                finalPos = finalPos.Except(mf64cols);

                            finalPos = finalPos.ToList();

                            HashSet<byte> except = new HashSet<byte>();

                            List<byte> mf64Pos = finalPos
                                .Where(j => j < 90)
                                .Select(j => Converter.XYtoMF64(j))
                                .ToList();

                            mf64Pos = mf64Pos.Select(j => {
                                byte _j = (byte)(~j & 0b00111111);

                                if (j < 32 && mf64Pos.Contains(_j)) {
                                    except.Add(_j);

                                    byte m1 = (byte)((j & 0b00011100) | (_j & 0b00100011));
                                    byte m2 = (byte)((j & 0b00100011) | (_j & 0b00011100));

                                    if (j < 16 && mf64Pos.Contains(m1) && mf64Pos.Contains(m2)) {
                                        except.Add((byte)(m2 | 0b01000000));
                                        except.Add(m1);
                                        except.Add(m2);
                                        return (byte)(~j & 0b01111111);
                                    }

                                    return (byte)(j | 0b01000000);
                                }
                                
                                return j;
                            }).ToList();

                            finalPos = finalPos
                                .Where(j => j >= 90)
                                .Concat(mf64Pos.Except(except));

                        } else {
                            if (positions.Intersect(allMap).Count() == 100 - excludedIndexes[Type].Count + Convert.ToInt32(excludedIndexes[Type].Contains(100))) {
                                finalPos = finalPos.Append((byte)0);
                                if (Type.IsPro() && positions.Contains((byte)99)) finalPos = finalPos.Append((byte)99); // Mode light
                            
                            } else {
                                finalPos = positions.ToList();

                                for (int j = Convert.ToInt32(!Type.Is10x10()); j < 10; j++) {
                                    IEnumerable<byte> row = rowMap(j);
                                    IEnumerable<byte> col = colMap(j);

                                    if (positions.Intersect(row).Count() == 8)
                                        finalPos = finalPos.Except(row).Append((byte)(100 + j));

                                    if (positions.Intersect(col).Count() == 8)
                                        finalPos = finalPos.Except(col).Append((byte)(110 + j));
                                }

                                if (finalPos.Intersect(squares).Count() == 16)
                                    finalPos = finalPos.Except(cols);
                            }
                        }
                        
                        if (finalPos.Count() > 7) chunk.Add((byte)finalPos.Count());
                        else for (int j = 0; j < 3; j++)
                            chunk[j] = (byte)(chunk[j] | ((finalPos.Count() & (4 >> j)) > 0? 0x40 : 0));

                        chunk.AddRange(finalPos);
                        
                        return chunk;
                    })
                )
                .ToList();

            return Type.IsPro()? ret.Count() <= 319 : true; // CFW Hard limit is 320 but this doesn't include SysExEnd
        }

        void RGBSend(List<RawUpdate> rgb) {
            IEnumerable<byte> colorspec = Type.IsGenerationX()? new byte[] { 0x03 } : Enumerable.Empty<byte>();

            SysExSend(RGBHeader[Type].Concat(rgb.SelectMany(i => colorspec.Concat(new byte[] {
                i.Index,
                (byte)(i.Color.Red * (Type.IsGenerationX()? 2 : 1)),
                (byte)(i.Color.Green * (Type.IsGenerationX()? 2 : 1)),
                (byte)(i.Color.Blue * (Type.IsGenerationX()? 2 : 1))
            }))));
        }

        public virtual void Clear(bool manual = false) {
            if (!Usable || (manual && PatternWindow != null)) return;

            CreateScreen();
        }

        public virtual void ForceClear() {
            if (!Usable) return;
            
            CreateScreen();

            SysExSend(ForceClearMessage[Type]);
            
            if (Type.IsPro()) RGBSend(new List<RawUpdate>() {
                new RawUpdate(99, new Color(0))
            });
        }

        public virtual void Render(Signal n) {
            if (PatternWindow == null || n.Origin == PatternWindow)
                screen?.MIDIEnter(n);
        }

        void StartIdentification() {
            Task.Run(() => {
                Available = true;
                SupportsCompression = false;

                Input.Received += WaitForIdentification;
                Output.Send(DeviceInquiry);
            });
        }

        public Launchpad() => CreateScreen();

        public Launchpad(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) {
            Input = input.CreateDevice();
            Output = output.CreateDevice();

            Name = input.Name;

            Input.Open();
            Output.Open();

            Program.Log($"MIDI Created {Name}");

            StartIdentification();
        }

        public Launchpad(string name, InputType format = InputType.DrumRack, RotationType rotation = RotationType.D0) {
            CreateScreen();
            
            Name = name;
            _format = format;
            _rotation = rotation;

            Available = false;
        }

        public virtual void Connect(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) {
            Input = input.CreateDevice();
            Output = output.CreateDevice();

            Input.Open();
            Output.Open();

            Program.Log($"MIDI Connected {Name}");

            Type = LaunchpadType.Unknown;
            doingMK2VersionInquiry = false;

            StartIdentification();
        }

        public virtual void Disconnect(bool actuallyClose = true) {
            if (actuallyClose) {
                if (Input.IsOpen) Input.Close();
                Input.Dispose();

                if (Output.IsOpen) Output.Close();
                Output.Dispose();
            }

            Dispatcher.UIThread.InvokeAsync(() => Window?.Close());

            Program.Log($"MIDI Disconnected {Name}");

            Available = false;
        }

        public void Reconnect() {
            if (!Usable || this.GetType() != typeof(Launchpad)) return;

            IMidiInputDeviceInfo input = MidiDeviceManager.Default.InputDevices.FirstOrDefault(i => i.Name == Input.Name);
            IMidiOutputDeviceInfo output = MidiDeviceManager.Default.OutputDevices.FirstOrDefault(o => o.Name == Output.Name);

            if (input == null || output == null) return;

            Input.Close();
            Output.Close();

            Input.Dispose();
            Output.Dispose();

            Input = input.CreateDevice();
            Output = output.CreateDevice();

            Input.Open();
            Output.Open();

            ForceClear();

            Input.Received += HandleMidi;
        }

        public void HandleSignal(Signal n, bool rotated = false) {
            if (Available && Program.Project != null) {
                if (!rotated && n.Index != 100) {
                    if (Rotation == RotationType.D90) n.Index = (byte)((9 - n.Index % 10) * 10 + n.Index / 10);
                    else if (Rotation == RotationType.D180) n.Index = (byte)((9 - n.Index / 10) * 10 + 9 - n.Index % 10);
                    else if (Rotation == RotationType.D270) n.Index = (byte)((n.Index % 10) * 10 + 9 - n.Index / 10);
                }

                if (PatternWindow == null) {
                    if (n.Color.Lit) {
                        if (inputbuffer[n.Index] == null)
                            inputbuffer[n.Index] = (int[])Program.Project.Macros.Clone();
                        else return;
                            
                    } else if (inputbuffer[n.Index] == null) return;
                    
                    n.Macros = (int[])inputbuffer[n.Index].Clone();

                    if (!n.Color.Lit) inputbuffer[n.Index] = null;

                    Receive?.Invoke(n);
                } else PatternWindow.MIDIEnter(n);
            }
        }

        public void HandleSignal(byte key, byte vel)
            => HandleSignal(new Signal(
                this,
                this,
                key,
                new Color((byte)Math.Max(Convert.ToInt32(vel > 0), vel >> 1))
            ));

        void HandleMidi(MidiMessage e) {
            if (e.IsNote) HandleNote(e.Pitch, e.IsNoteOff? (byte)0 : e.Velocity);
            else if (e.IsCC) HandleCC(e.Pitch, e.Velocity);
        }

        public void HandleNote(byte key, byte vel) {
            bool xy = false;

            if (InputFormat == InputType.DrumRack) {
                // X and Mini MK3 old Programmer mode hack note handling
                if (Type.HasProgrammerFwHack()) {
                    if (108 <= key && key <= 115) key -= 80;
                    else if (key == 116) key = 27;

                } else if (Type == LaunchpadType.ProMK3) {
                    xy = true;
                    if (key == 124) key = 90;  // Shift key
                    else if (12 <= key && key <= 19) key -= 11;  // Extra bottom row
                    else xy = false;
                }
            }

            if (!xy) key = (InputFormat == InputType.DrumRack)? Converter.DRtoXY(key) : ((key == 99)? (byte)100 : key);

            HandleSignal(key, vel);
        }

        void HandleCC(byte key, byte vel) {
            // TODO handle Multi Reset more universally
            switch (Type) {
                case LaunchpadType.MK2:
                    if (104 <= key && key <= 111)
                        HandleSignal(key -= 13, vel);
                    break;

                case LaunchpadType.Pro:
                case LaunchpadType.CFW:
                    if (key == 121) {
                        Multi.InvokeReset();
                        return;
                    }

                    HandleSignal(key, vel);
                    break;

                case LaunchpadType.X:
                case LaunchpadType.MiniMK3:
                    HandleSignal(key, vel);
                    break;

                case LaunchpadType.ProMK3:
                    if (101 <= key && key <= 108)
                        key -= 100;

                    HandleSignal(key, vel);
                    break;
                
                case LaunchpadType.MatrixFE:
                case LaunchpadType.Matrix:
                case LaunchpadType.MatrixPro:
                    if (key == 121)
                        Multi.InvokeReset();
                    break;
            }
        }

        public override string ToString() => (Available? "" : "(unavailable) ") + Name;
    }
}
