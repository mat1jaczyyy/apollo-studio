using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Messages;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Launchpad {
        public static PortWarning MK2FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad MK2s are running an older version of the\n" + 
            "official Novation firmware which is not compatible with Apollo Studio.\n\n" +
            "Update these to the latest version of the firmware to use them with Apollo\n" +
            "Studio.",
            "Download Official Updater",
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "https://customer.novationmusic.com/sites/customer/files/novation/downloads/13333/launchpad-mk2-updater.exe"
                : "https://customer.novationmusic.com/sites/customer/files/novation/downloads/13333/launchpad-mk2-updater-1.0.dmg"
        );

        public static PortWarning ProFirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running an older version of the\n" + 
            "official Novation firmware. While they will work with Apollo Studio, this\n" +
            "version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the official\n" +
            "Novation updater to avoid any potential issues with Apollo Studio.",
            "Download Official Updater",
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "https://customer.novationmusic.com/sites/customer/files/novation/downloads/15481/launchpad-pro-updater-1.2.exe"
                : "https://customer.novationmusic.com/sites/customer/files/novation/downloads/15481/launchpad-pro-updater-1.2.dmg"
        );

        public static PortWarning CFWIncompatible { get; private set; } = new PortWarning(
            "One or more connected Launchpad Pros are running an older version of the\n" + 
            "performance-optimized custom firmware which is not compatible with\n" +
            "Apollo Studio.\n\n" +
            "Update these to the latest version of the firmware or switch back to stock\n" +
            "firmware to use them with Apollo Studio.",
            "Custom Firmware Main Page",
            "https://github.com/mat1jaczyyy/lpp-performance-cfw"
        );

        public static PortWarning XFirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Xs are running an older version of the\n" + 
            "official Novation firmware. While they will work with Apollo Studio, this\n" +
            "version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the Novation\n" +
            "Components app to avoid any potential issues with Apollo Studio.",
            "Launch Components Online (requires Chrome)",
            "https://circuit-librarian-staging.herokuapp.com/launchpad-x/firmware"
        );

        public static PortWarning MiniMK3FirmwareOld { get; private set; } = new PortWarning(
            "One or more connected Launchpad Mini MK3s are running an older version of the\n" + 
            "official Novation firmware. While they will work with Apollo Studio, this\n" +
            "version is known to cause performance issues and lags.\n\n" +
            "Update these to the latest version of the firmware using the Novation\n" +
            "Components app to avoid any potential issues with Apollo Studio.",
            "Launch Components Online (requires Chrome)",
            "https://circuit-librarian-staging.herokuapp.com/launchpad-mini-mk3/firmware"
        );

        public static void DisplayWarnings(Window sender) {
            Dispatcher.UIThread.Post(() => {
                if (MK2FirmwareOld.DisplayWarning(sender)) return;
                if (ProFirmwareOld.DisplayWarning(sender)) return;
                if (CFWIncompatible.DisplayWarning(sender)) return;
                if (XFirmwareOld.DisplayWarning(sender)) return;
                MiniMK3FirmwareOld.DisplayWarning(sender);
            }, DispatcherPriority.MinValue);
        }

        public LaunchpadWindow Window;

        PatternWindow _patternwindow;
        public virtual PatternWindow PatternWindow {
            get => _patternwindow;
            set => _patternwindow = value;
        }

        public List<AbletonLaunchpad> AbletonLaunchpads = new List<AbletonLaunchpad>();

        IMidiInputDevice Input;
        IMidiOutputDevice Output;

        public LaunchpadType Type { get; protected set; } = LaunchpadType.Unknown;

        bool HasModeLight => Type == LaunchpadType.PRO || Type == LaunchpadType.CFW;
        bool IsGenerationX => Type == LaunchpadType.X || Type == LaunchpadType.MiniMK3;

        static byte[] NovationHeader = new byte[] {0x00, 0x20, 0x29, 0x02};

        static Dictionary<LaunchpadType, byte[]> RGBHeader = new Dictionary<LaunchpadType, byte[]>() {
            {LaunchpadType.MK2, NovationHeader.Concat(new byte[] {0x18, 0x0B}).ToArray()},
            {LaunchpadType.PRO, NovationHeader.Concat(new byte[] {0x10, 0x0B}).ToArray()},
            {LaunchpadType.CFW, new byte[] {0x6F}},
            {LaunchpadType.X, NovationHeader.Concat(new byte[] {0x0C, 0x03, 0x03}).ToArray()},
            {LaunchpadType.MiniMK3, NovationHeader.Concat(new byte[] {0x0D, 0x03, 0x03}).ToArray()}
        };

        static Dictionary<LaunchpadType, byte[]> ClearMessage = new Dictionary<LaunchpadType, byte[]>() {
            {LaunchpadType.MK2, NovationHeader.Concat(new byte[] {0x18, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.PRO, NovationHeader.Concat(new byte[] {0x10, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.CFW, NovationHeader.Concat(new byte[] {0x10, 0x0E, 0x00}).ToArray()},
            {LaunchpadType.X, NovationHeader.Concat(new byte[] {0x0C, 0x02, 0x00}).ToArray()},
            {LaunchpadType.MiniMK3, NovationHeader.Concat(new byte[] {0x0D, 0x02, 0x00}).ToArray()}
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

        public delegate void ReceiveEventHandler(Signal n);
        public event ReceiveEventHandler Receive;

        protected void InvokeReceive(Signal n) => Receive?.Invoke(n);

        protected Screen screen;
        ConcurrentQueue<SysExMessage> buffer;
        object locker;
        int[][] inputbuffer;
        ulong signalCount = 0;

        protected void CreateScreen() {
            screen = new Screen() { ScreenExit = Send };
            buffer = new ConcurrentQueue<SysExMessage>();
            locker = new object();
            inputbuffer = (from i in Enumerable.Range(0, 101) select new int[4] {0, 0, 0, 0}).ToArray();
        }

        public Color GetColor(int index) => (PatternWindow == null)
            ? screen.GetColor(index)
            : PatternWindow.Device.Frames[PatternWindow.Device.Expanded].Screen[index].Clone();

        readonly static SysExMessage DeviceInquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});
        readonly static SysExMessage VersionInquiry = new SysExMessage(new byte[] {0x00, 0x20, 0x29, 0x00, 0x70});

        bool doingMK2VersionInquiry = false;

        LaunchpadType AttemptIdentify(SysExMessage response) {
            if (doingMK2VersionInquiry) {
                doingMK2VersionInquiry = false;

                if (response.Data.Length != 17)
                    return LaunchpadType.Unknown;
                
                if (response.Data[0] == 0x00 && response.Data[1] == 0x20 && response.Data[2] == 0x29 && response.Data[3] == 0x00 && response.Data[4] == 0x70) {
                    int versionInt = int.Parse(string.Join("", response.Data.SkipLast(2).TakeLast(3)));

                    if (versionInt < 171) // Old Firmware
                        MK2FirmwareOld.Set();
                    
                    return LaunchpadType.Unknown; // Bootloader
                }
                
                return LaunchpadType.Unknown;
            }

            if (response.Data.Length != 15)
                return LaunchpadType.Unknown;

            if (response.Data[0] != 0x7E || response.Data[2] != 0x06 || response.Data[3] != 0x02)
                return LaunchpadType.Unknown;

            if (response.Data[4] == 0x00 && response.Data[5] == 0x20 && response.Data[6] == 0x29) { // Manufacturer = Novation
                string versionStr = string.Join("", response.Data.TakeLast(3).Select(i => (char)i));
                int versionInt = int.Parse(string.Join("", response.Data.TakeLast(3)));

                switch (response.Data[7]) {
                    case 0x69: // Launchpad MK2
                        if (versionInt < 171) { // Old Firmware or Bootloader?
                            doingMK2VersionInquiry = true;
                            return LaunchpadType.Unknown;
                        }

                        return LaunchpadType.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (versionStr == "\0\0\0") // Bootloader
                            return LaunchpadType.Unknown;

                        if (versionStr == "cfw") { // Old Custom Firmware
                            CFWIncompatible.Set();
                            return LaunchpadType.Unknown;
                        }

                        if (versionStr == "cfx") // Custom Firmware
                            return LaunchpadType.CFW;
                        
                        if (versionInt < 182) // Old Firmware
                            ProFirmwareOld.Set();

                        return LaunchpadType.PRO;

                    case 0x03: // Launchpad X
                        if (response.Data[8] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 289) // Old Firmware
                            XFirmwareOld.Set();

                        return LaunchpadType.X;
                    
                    case 0x13: // Launchpad Mini MK3
                        if (response.Data[8] == 17) // Bootloader
                            return LaunchpadType.Unknown;
                        
                        if (versionInt < 341) // Old Firmware
                            MiniMK3FirmwareOld.Set();

                        return LaunchpadType.MiniMK3;
                }
            }

            return LaunchpadType.Unknown;
        }

        void WaitForIdentification(object sender, in SysExMessage e) {
            if ((Type = AttemptIdentify(e)) != LaunchpadType.Unknown) {
                Input.SysEx -= WaitForIdentification;

                Clear();

                Input.NoteOn += NoteOn;
                Input.NoteOff += NoteOff;
                Input.ControlChange += ControlChange;

                MIDI.DoneIdentifying();

            } else {
                Task.Delay(1000).ContinueWith(_ => {
                    if (Available && Type == LaunchpadType.Unknown) {
                        if (doingMK2VersionInquiry) Output.Send(in VersionInquiry);
                        else Output.Send(in DeviceInquiry);
                    }
                });
            }
        }

        bool SysExSend(byte[] raw) {
            if (!Available || Type == LaunchpadType.Unknown) return false;

            buffer.Enqueue(new SysExMessage(raw));
            ulong current = signalCount;

            Task.Run(() => {
                lock (locker) {
                    if (buffer.TryDequeue(out SysExMessage msg))
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

        public virtual void Send(Signal n) {
            if (!Available || Type == LaunchpadType.Unknown) return;

            Signal m = n.Clone();
            Window?.SignalRender(m);

            foreach (AbletonLaunchpad alp in AbletonLaunchpads)
                alp.Window?.SignalRender(m);

            n = n.Clone();

            byte r = (byte)(n.Color.Red * (IsGenerationX? 2 : 1));
            byte g = (byte)(n.Color.Green * (IsGenerationX? 2 : 1));
            byte b = (byte)(n.Color.Blue * (IsGenerationX? 2 : 1));

            if (n.Index != 100) {
                if (Rotation == RotationType.D90) n.Index = (byte)((n.Index % 10) * 10 + 9 - n.Index / 10);
                else if (Rotation == RotationType.D180) n.Index = (byte)((9 - n.Index / 10) * 10 + 9 - n.Index % 10);
                else if (Rotation == RotationType.D270) n.Index = (byte)((9 - n.Index % 10) * 10 + n.Index / 10);
            }

            int offset = 0;

            switch (Type) {
                case LaunchpadType.MK2:
                    if (n.Index % 10 == 0 || n.Index < 11 || n.Index == 99 || n.Index == 100) return;
                    if (91 <= n.Index && n.Index <= 98) offset = 13;
                    break;
                
                case LaunchpadType.PRO:
                case LaunchpadType.CFW:
                    if (n.Index == 0 || n.Index == 9 || n.Index == 90 || n.Index == 99) return;
                    else if (n.Index == 100) offset = -1;
                    break;

                case LaunchpadType.X:
                case LaunchpadType.MiniMK3:
                    if (n.Index % 10 == 0 || n.Index < 11 || n.Index == 100) return;
                    break;
            }

            SysExSend(RGBHeader[Type].Concat(new byte[] {(byte)(n.Index + offset), r, g, b}).ToArray());
        }

        public virtual void Clear(bool manual = false) {
            if (!Available || Type == LaunchpadType.Unknown || (manual && PatternWindow != null)) return;

            CreateScreen();

            Signal n = new Signal(this, this, 0, new Color(0));

            for (int i = 0; i < 101; i++) {
                n.Index = (byte)i;
                Window?.SignalRender(n.Clone());
            }

            SysExSend(ClearMessage[Type]);
            
            if (HasModeLight) Send(n); // Clear Mode Light
        }

        public virtual void Render(Signal n) {
            if (PatternWindow == null || n.Origin == PatternWindow)
                screen?.MIDIEnter(n);
        }

        public Launchpad() => CreateScreen();

        public Launchpad(IMidiInputDeviceInfo input, IMidiOutputDeviceInfo output) {
            Input = input.CreateDevice();
            Output = output.CreateDevice();

            Name = input.Name;

            Input.Open();
            Output.Open();

            Program.Log($"MIDI Created {Name}");

            Available = true;

            Input.SysEx += WaitForIdentification;
            Output.Send(in DeviceInquiry);
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

            Available = true;
            Type = LaunchpadType.Unknown;
            doingMK2VersionInquiry = false;

            Input.SysEx += WaitForIdentification;
            Output.Send(in DeviceInquiry);
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
            if (this.GetType() != typeof(Launchpad) || Type == LaunchpadType.Unknown || !Available) return;

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

            Clear();

            Input.NoteOn += NoteOn;
            Input.NoteOff += NoteOff;
            Input.ControlChange += ControlChange;
        }

        public void HandleMessage(Signal n, bool rotated = false) {
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
                            
                    } else if (inputbuffer[n.Index] == null) return;
                    
                    n.Macros = (int[])inputbuffer[n.Index].Clone();

                    if (!n.Color.Lit) inputbuffer[n.Index] = null;

                    Receive?.Invoke(n);
                } else PatternWindow.MIDIEnter(n);
            }
        }

        byte InputColor(int input) => (byte)(Math.Max(Convert.ToInt32(input > 0), input >> 1));

        public void NoteOn(object sender, in NoteOnMessage e) => HandleMessage(new Signal(
            InputFormat,
            this,
            this,
            (byte)e.Key,
            new Color(InputColor(e.Velocity))
        ));

        void NoteOff(object sender, in NoteOffMessage e) => HandleMessage(new Signal(
            InputFormat,
            this,
            this,
            (byte)e.Key,
            new Color(0)
        ));

        void ControlChange(object sender, in ControlChangeMessage e) {
            switch (Type) {
                case LaunchpadType.MK2:
                    if (104 <= e.Control && e.Control <= 111)
                        HandleMessage(new Signal(
                            InputType.XY,
                            this,
                            this,
                            (byte)(e.Control - 13),
                            new Color(InputColor(e.Value))
                        ));
                    break;

                case LaunchpadType.PRO:
                case LaunchpadType.CFW:
                    if (e.Control == 121) {
                        Multi.InvokeReset();
                        return;
                    }

                    HandleMessage(new Signal(
                        InputType.XY,
                        this,
                        this,
                        (byte)e.Control,
                        new Color(InputColor(e.Value))
                    ));
                    break;

                case LaunchpadType.X:
                case LaunchpadType.MiniMK3:
                    HandleMessage(new Signal(
                        InputType.XY,
                        this,
                        this,
                        (byte)e.Control,
                        new Color(InputColor(e.Value))
                    ));
                    break;
            }
        }

        public override string ToString() => (Available? "" : "(unavailable) ") + Name;
    }
}
