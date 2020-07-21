using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Apollo.Elements {
    public class Launchpad {
        public static PortWarning MK2FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad MK2s are running an older version of the\n" + 
            "official Novation firmware which is not compatible with Apollo Studio.\n\n" +
            "Update these to the latest version of the firmware using the official updater or\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Download Official Updater",
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "https://customer.novationmusic.com/sites/customer/files/novation/downloads/13333/launchpad-mk2-updater.exe"
                    : "https://customer.novationmusic.com/sites/customer/files/novation/downloads/13333/launchpad-mk2-updater-1.0.dmg",
                false
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning ProFirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running an older version of the\n" + 
            "official Novation firmware. While they will work with Apollo Studio, this\n" +
            "version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the official updater or\n" +
            "Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Download Official Updater",
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "https://customer.novationmusic.com/sites/customer/files/novation/downloads/15481/launchpad-pro-updater-1.2.exe"
                    : "https://customer.novationmusic.com/sites/customer/files/novation/downloads/15481/launchpad-pro-updater-1.2.dmg",
                false
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning CFWIncompatible { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running an older version of the\n" + 
            "performance-optimized custom firmware which is not compatible with\n" +
            "Apollo Studio.\n\n" +
            "Update these to the latest version of the firmware using\n" +
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
            "Update these to the latest version of the firmware using Novation Components\n" +
            "or Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-x/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning MiniMK3FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Mini MK3s are running an older version of\n" + 
            "the official Novation firmware. While they will work with Apollo Studio,\n" +
            "this version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using Novation Components\n" +
            "or Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-mini-mk3/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning ProMK3FirmwareUnsupported { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pro MK3s are running an older version of\n" + 
            "the official Novation firmware which is not compatible with \n" +
            "Apollo Studio due to not having a dedicated Programmer mode.\n\n" +
            "Update these to the latest version of the firmware using Novation Components\n" +
            "or Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-mini-mk3/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static PortWarning ProMK3FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pro MK3s are running an older version of\n" + 
            "the official Novation firmware. While they will work with Apollo Studio,\n" +
            "this version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using Novation Components\n" +
            "or Launchpad Firmware Utility to avoid any potential issues with Apollo Studio.",
            new PortWarning.Option(
                "Launch Components Online",
                "https://components.novationmusic.com/launchpad-mini-mk3/firmware"
            ),
            new PortWarning.Option(
                "Launch Firmware Utility",
                "https://fw.mat1jaczyyy.com"
            )
        );

        public static void DisplayWarnings(Window sender) {
            Dispatcher.UIThread.Post(() => {
                if (MK2FirmwareOld.DisplayWarning(sender)) return;
                if (ProFirmwareOld.DisplayWarning(sender)) return;
                if (CFWIncompatible.DisplayWarning(sender)) return;
                if (XFirmwareOld.DisplayWarning(sender)) return;
                if (MiniMK3FirmwareOld.DisplayWarning(sender)) return;
                if (ProMK3FirmwareUnsupported.DisplayWarning(sender)) return;
                ProMK3FirmwareOld.DisplayWarning(sender);
            }, DispatcherPriority.MinValue);
        }

        public LaunchpadWindow Window;
        public LaunchpadInfo Info;

        PatternWindow _patternwindow;
        public virtual PatternWindow PatternWindow {
            get => _patternwindow;
            set => _patternwindow = value;
        }

        public List<AbletonLaunchpad> AbletonLaunchpads = new List<AbletonLaunchpad>();

        IMidiInputDevice Input;
        IMidiOutputDevice Output;

        public LaunchpadType Type { get; protected set; } = LaunchpadType.Unknown;

        static byte[] SysExStart = new byte[] { 0xF0 };
        static byte[] SysExEnd = new byte[] { 0xF7 };
        static byte[] NovationHeader = new byte[] {0x00, 0x20, 0x29, 0x02};

        static Dictionary<LaunchpadType, byte[]> RGBHeader = new Dictionary<LaunchpadType, byte[]>() {
            {LaunchpadType.MK2, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x18, 0x0B}).ToArray()},
            {LaunchpadType.Pro, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0B}).ToArray()},
            {LaunchpadType.CFW, SysExStart.Concat(new byte[] {0x6F}).ToArray()},
            {LaunchpadType.X, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0C, 0x03}).ToArray()},
            {LaunchpadType.MiniMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0D, 0x03}).ToArray()},
            {LaunchpadType.ProMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0E, 0x03}).ToArray()}
        };

        static Dictionary<LaunchpadType, byte[]> ForceClearMessage = new Dictionary<LaunchpadType, byte[]>() {
            {LaunchpadType.MK2, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x18, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.Pro, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.CFW, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x10, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.X, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0C, 0x02, 0x00}).ToArray()},
            {LaunchpadType.MiniMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0D, 0x02, 0x00}).ToArray()},
            {LaunchpadType.ProMK3, SysExStart.Concat(NovationHeader).Concat(new byte[] {0x0E, 0x03}).Concat(
                Enumerable.Range(0, 109).SelectMany(i => new byte[] {0x00, (byte)i, 0x00})
            ).ToArray()}
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
            : PatternWindow.Device.Frames[PatternWindow.Device.Expanded].Screen[index].Clone();

        static readonly byte[] DeviceInquiry = new byte[] {0xF0, 0x7E, 0x7F, 0x06, 0x01, 0xF7};
        static readonly byte[] VersionInquiry = new byte[] {0xF0, 0x00, 0x20, 0x29, 0x00, 0x70, 0xF7};

        bool doingMK2VersionInquiry = false;

        LaunchpadType AttemptIdentify(MidiMessage response) {
            if (doingMK2VersionInquiry) {
                doingMK2VersionInquiry = false;

                if (response.Data.Length != 19)
                    return LaunchpadType.Unknown;
                
                if (response.CheckSysExHeader(new byte[] {0x00, 0x20, 0x29, 0x00}) && response.Data[5] == 0x70) {
                    int versionInt = int.Parse(string.Join("", response.Data.SkipLast(2).TakeLast(3)));

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

            if (response.Data[5] == 0x00 && response.Data[6] == 0x20 && response.Data[7] == 0x29) { // Manufacturer = Novation
                IEnumerable<byte> version = response.Data.SkipLast(1).TakeLast(3);
                string versionStr = string.Join("", version.Select(i => (char)i));
                int versionInt = int.Parse(string.Join("", version));

                switch (response.Data[8]) {
                    case 0x69: // Launchpad MK2
                        if (versionInt < 171) { // Old Firmware or Bootloader?
                            doingMK2VersionInquiry = true;
                            return LaunchpadType.Unknown;
                        }

                        return LaunchpadType.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (versionStr == "\0\0\0") // Bootloader
                            return LaunchpadType.Unknown;

                        if (versionStr == "cfw" || versionStr == "cfx") { // Old Custom Firmware
                            CFWIncompatible.Set();
                            return LaunchpadType.Unknown;
                        }

                        if (versionStr == "cfy") // Custom Firmware
                            return LaunchpadType.CFW;
                        
                        if (versionInt < 182) // Old Firmware
                            ProFirmwareOld.Set();

                        return LaunchpadType.Pro;

                    case 0x03: // Launchpad X
                        if (response.Data[9] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 351) // Old Firmware
                            XFirmwareOld.Set();

                        return LaunchpadType.X;
                    
                    case 0x13: // Launchpad Mini MK3
                        if (response.Data[9] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 407) // Old Firmware
                            MiniMK3FirmwareOld.Set();

                        return LaunchpadType.MiniMK3;
                    
                    case 0x23: // Launchpad Pro MK3
                        if (response.Data[9] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 440) { // No Programmer mode
                            ProMK3FirmwareUnsupported.Set();
                            return LaunchpadType.Unknown;
                        }
                        
                        if (versionInt < 450) // Old Firmware
                            ProMK3FirmwareOld.Set();

                        return LaunchpadType.ProMK3;
                }
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

        bool SysExSend(IEnumerable<byte> raw) {
            byte[] bytes = raw.Concat(SysExEnd).ToArray();

            if (!Usable) return false;

            buffer.Enqueue(bytes);
            ulong current = signalCount;

            Task.Run(() => {
                lock (locker) {
                    if (buffer.TryDequeue(out byte[] msg))
                        Output.Send(msg);
                    
                    signalCount++;
                }
            });

            // This protects from deadlock for some reason
            Task.Delay(1000).ContinueWith(_ => {
                if (signalCount <= current)
                    Disconnect(false);
            });

            return true;
        }

        public void Send(Signal n) => Send(new List<Signal>() {n});
        
        public virtual void Send(List<Signal> n) {
            if (!n.Any() || !Usable) return;

            // TODO Heaven Grid/Row/All optimization? Likely to be extremely device specific...

            List<byte[]> output = n.SelectMany(i => {
                Signal m = i.Clone();
                Window?.SignalRender(m);

                foreach (AbletonLaunchpad alp in AbletonLaunchpads)
                    alp.Window?.SignalRender(m);

                i = i.Clone();  // TODO might be unnecessary

                if (i.Index != 100) {
                    if (Rotation == RotationType.D90) i.Index = (byte)((i.Index % 10) * 10 + 9 - i.Index / 10);
                    else if (Rotation == RotationType.D180) i.Index = (byte)((9 - i.Index / 10) * 10 + 9 - i.Index % 10);
                    else if (Rotation == RotationType.D270) i.Index = (byte)((9 - i.Index % 10) * 10 + i.Index / 10);
                }

                IEnumerable<byte[]> ret = Enumerable.Empty<byte[]>();

                int offset = 0;

                switch (Type) {
                    case LaunchpadType.MK2:
                        if (i.Index % 10 == 0 || i.Index < 11 || i.Index == 99 || i.Index == 100) return ret;
                        if (91 <= i.Index && i.Index <= 98) offset = 13;
                        break;
                    
                    case LaunchpadType.Pro:
                    case LaunchpadType.CFW:
                        if (i.Index == 0 || i.Index == 9 || i.Index == 90 || i.Index == 99) return ret;
                        else if (i.Index == 100) offset = -1;
                        break;

                    case LaunchpadType.X:
                    case LaunchpadType.MiniMK3:
                        if (i.Index % 10 == 0 || i.Index < 11 || i.Index == 100) return ret;
                        break;
                        
                    case LaunchpadType.ProMK3:
                        if (i.Index == 0 || i.Index == 9 || i.Index == 100) return ret;
                        break;
                }

                ret = new [] { RGBCreate(i, offset) };

                if (Type == LaunchpadType.ProMK3 && 1 <= i.Index && i.Index <= 8)
                    ret = ret.Append(RGBCreate(i, 100));
                
                return ret.ToArray();
            }).ToList();

            // TODO Heaven CFW compatibility - a CFW update will be required for this, because dumb me never implemented fast multimessaging

            if (Type == LaunchpadType.Pro) {
                // https://customer.novationmusic.com/sites/customer/files/novation/downloads/10598/launchpad-pro-programmers-reference-guide_0.pdf
                // Page 22: The <LED> <Red> <Green> <Blue> group may be repeated in the message up to 78 times.
                for (int i = 0; i < output.Count; i += 78)
                    RGBSend(output.Skip(i).Take(78).ToList());
            
            } else RGBSend(output);
        }

        byte[] RGBCreate(Signal n, int offset = 0) {
            IEnumerable<byte> ret = Type.IsGenerationX()? new byte[] { 0x03 } : Enumerable.Empty<byte>();

            return ret.Concat(new byte[] {
                (byte)(n.Index + offset),
                (byte)(n.Color.Red * (Type.IsGenerationX()? 2 : 1)),
                (byte)(n.Color.Green * (Type.IsGenerationX()? 2 : 1)),
                (byte)(n.Color.Blue * (Type.IsGenerationX()? 2 : 1))
            }).ToArray();
        }

        void RGBSend(List<byte[]> rgb) => SysExSend(RGBHeader[Type].Concat(rgb.SelectMany(i => i)));

        public virtual void Clear(bool manual = false) {
            if (!Usable || (manual && PatternWindow != null)) return;

            CreateScreen();
        }

        public virtual void ForceClear() {
            if (!Usable) return;
            
            CreateScreen();

            SysExSend(ForceClearMessage[Type]);
            
            if (Type.HasModeLight()) RGBSend(new List<byte[]>() {
                RGBCreate(new Signal(null, this, 100, new Color(0)), -1)
            });
        }

        public virtual void Render(Signal n) {
            if (PatternWindow == null || n.Origin == PatternWindow)
                screen?.MIDIEnter(n);
        }

        void StartIdentification() {
            Task.Run(() => {
                Available = true;

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

        public void HandleSignal(byte key, byte vel, InputType format = InputType.XY)
            => HandleSignal(new Signal(
                format,
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
            // X and Mini MK3 old Programmer mode hack note handling
            if (Type.HasProgrammerFwHack() && InputFormat == InputType.DrumRack) {
                if (108 <= key && key <= 115) key -= 80;
                else if (key == 116) key = 27;
            }

            HandleSignal(key, vel, InputFormat);
        }

        void HandleCC(byte key, byte vel) {
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
            }
        }

        public override string ToString() => (Available? "" : "(unavailable) ") + Name;
    }
}
