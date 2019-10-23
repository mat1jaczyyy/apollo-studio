using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Messages;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Launchpad {
        public static CFWIncompatibleState CFWIncompatible = CFWIncompatibleState.None;

        public static void CFWError(Window sender) {
            CFWIncompatible = CFWIncompatibleState.Done;

            Dispatcher.UIThread.Post(async () => await MessageWindow.Create(
                "One or more connected Launchpad Pros are running an older version of the\n" + 
                "performance-optimized custom firmware which is not compatible with\n" +
                "Apollo Studio.\n\n" +
                "Update these to the latest version of the firmware or switch back to stock\n" +
                "firmware to use them with Apollo Studio.",
                null, sender
            ), DispatcherPriority.MinValue);
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

        bool IsGenerationX => Type == LaunchpadType.X || Type == LaunchpadType.MiniMK3;

        static byte[] NovationHeader = new byte[] {0x00, 0x20, 0x29, 0x02};

        static Dictionary<LaunchpadType, byte[]> RGBHeader = new Dictionary<LaunchpadType, byte[]>() {
            {LaunchpadType.MK2, NovationHeader.Concat(new byte[] {0x18, 0x0B}).ToArray()},
            {LaunchpadType.PRO, NovationHeader.Concat(new byte[] {0x10, 0x0B}).ToArray()},
            {LaunchpadType.CFW, new byte[] {0x6F}},
            {LaunchpadType.X, NovationHeader.Concat(new byte[] {0x0C, 0x03, 0x03}).ToArray()},
            {LaunchpadType.MiniMK3, NovationHeader.Concat(new byte[] {0x0C, 0x03, 0x03}).ToArray()}
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
        int[] inputbuffer;
        ulong signalCount = 0;

        protected void CreateScreen() {
            screen = new Screen() { ScreenExit = Send };
            buffer = new ConcurrentQueue<SysExMessage>();
            locker = new object();
            inputbuffer = (from i in Enumerable.Range(0, 101) select 0).ToArray();
        }

        public Color GetColor(int index) => (PatternWindow == null)
            ? screen.GetColor(index)
            : PatternWindow.Device.Frames[PatternWindow.Device.Expanded].Screen[index].Clone();

        readonly static SysExMessage Inquiry = new SysExMessage(new byte[] {0x7E, 0x7F, 0x06, 0x01});

        LaunchpadType AttemptIdentify(SysExMessage response) {
            if (response.Data.Length != 15)
                return LaunchpadType.Unknown;

            if (response.Data[0] != 0x7E || response.Data[2] != 0x06 || response.Data[3] != 0x02)
                return LaunchpadType.Unknown;

            if (response.Data[4] == 0x00 && response.Data[5] == 0x20 && response.Data[6] == 0x29) { // Manufacturer = Novation
                switch (response.Data[7]) {
                    case 0x69: // Launchpad MK2
                        return LaunchpadType.MK2;
                    
                    case 0x51: // Launchpad Pro
                        if (response.Data[12] == 'c' && response.Data[13] == 'f' && response.Data[14] == 'w') {
                            if (CFWIncompatible == CFWIncompatibleState.None) {
                                if (Application.Current != null && App.MainWindow != null) CFWError(null);
                                else CFWIncompatible = CFWIncompatibleState.Show;
                            }

                            break;
                        }
                        return (response.Data[12] == 'c' && response.Data[13] == 'f' && response.Data[14] == 'x')? LaunchpadType.CFW : LaunchpadType.PRO;

                    case 0x03: // Launchpad X
                        return LaunchpadType.X;
                    
                    case 0x13: // Launchpad Mini MK3
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
                    if (Available && Type == LaunchpadType.Unknown)
                        Output.Send(in Inquiry);
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
            if (n.Index == 0 || n.Index == 9 || n.Index == 90 || (!IsGenerationX && n.Index == 99)) return;

            Signal m = n.Clone();
            Window?.SignalRender(m);

            foreach (AbletonLaunchpad alp in AbletonLaunchpads)
                alp.Window?.SignalRender(m);

            if (n.Index != 100) {
                if (Rotation == RotationType.D90) n.Index = (byte)((n.Index % 10) * 10 + 9 - n.Index / 10);
                else if (Rotation == RotationType.D180) n.Index = (byte)((9 - n.Index / 10) * 10 + 9 - n.Index % 10);
                else if (Rotation == RotationType.D270) n.Index = (byte)((9 - n.Index % 10) * 10 + n.Index / 10);

            } else n.Index = 99;

            int offset = 0;
            if (Type == LaunchpadType.MK2 || IsGenerationX) {
                if (n.Index % 10 == 0 || n.Index < 11 || n.Index == 100) return;
                if (Type == LaunchpadType.MK2 && 91 <= n.Index && n.Index <= 98) offset = 13;
            }

            SysExSend(RGBHeader[Type].Concat(new byte[] {(byte)(n.Index + offset), n.Color.Red, n.Color.Green, n.Color.Blue}).ToArray());
        }

        public virtual void Clear(bool manual = false) {
            if (!Available || (manual && PatternWindow != null)) return;

            CreateScreen();

            Signal n = new Signal(this, this, 0, new Color(0));

            for (int i = 0; i < 101; i++) {
                n.Index = (byte)i;
                Window?.SignalRender(n.Clone());
            }

            //SysExSend(new byte[] {0x0E, 0x00});
            Send(n);
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
            Output.Send(in Inquiry);
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

            if (Type == LaunchpadType.Unknown) {
                Input.SysEx += WaitForIdentification;
                Output.Send(in Inquiry);
            } else {
                Clear();

                Input.NoteOn += NoteOn;
                Input.NoteOff += NoteOff;
                Input.ControlChange += ControlChange;
            }
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
                        if (inputbuffer[n.Index] == 0)
                            inputbuffer[n.Index] = Program.Project.Page;
                        
                    } else if (inputbuffer[n.Index] == 0) return;
                    
                    n.Page = inputbuffer[n.Index];

                    if (!n.Color.Lit) inputbuffer[n.Index] = 0;

                    Receive?.Invoke(n);
                } else PatternWindow.MIDIEnter(n);
            }
        }

        public void NoteOn(object sender, in NoteOnMessage e) => HandleMessage(new Signal(
            InputFormat,
            this,
            this,
            (byte)e.Key,
            new Color((byte)(e.Velocity >> 1))
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
                            new Color((byte)(e.Value >> 1))
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
                        new Color((byte)(e.Value >> 1))
                    ));
                    break;
            }
        }

        public override string ToString() => (Available? "" : "(unavailable) ") + Name;
    }
}