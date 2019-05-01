using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class PageSwitch: Device {
        public static readonly new string DeviceIdentifier = "pageswitch";

        private int _target = 1;
        public int Target {
            get => _target;
            set {
                if (1 <= value && value <= 100)
                    _target = value;
            }
        }

        public override Device Clone() => new PageSwitch(Target);

        public PageSwitch(int target = 1): base(DeviceIdentifier) => Target = target;

        public override void MIDIEnter(Signal n) {
            Program.Project.Page = Target;
            MIDIExit?.Invoke(n);
        }
    }
}