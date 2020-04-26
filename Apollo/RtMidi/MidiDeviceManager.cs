using System;
using System.Collections.Generic;

using Apollo.RtMidi.Devices.Infos;
using Apollo.RtMidi.Unmanaged;
using Apollo.RtMidi.Unmanaged.API;

namespace Apollo.RtMidi {
    public sealed class MidiDeviceManager: IDisposable {
        public static MidiDeviceManager Default => DefaultHolder.Value;

        private static readonly Lazy<MidiDeviceManager> DefaultHolder = new Lazy<MidiDeviceManager>(() => new MidiDeviceManager());

        private readonly RtMidiManager _rtDeviceManager;
        private bool _disposed;

        private MidiDeviceManager()
            => _rtDeviceManager = RtMidiManager.Default;

        ~MidiDeviceManager()
            => Dispose();

        // Enumerate all currently available input devices
        public IEnumerable<IMidiInputDeviceInfo> InputDevices {
            get {
                foreach (var rtInputDeviceInfo in _rtDeviceManager.InputDevices)
                    yield return new MidiInputDeviceInfo(rtInputDeviceInfo);
            }
        }

        // Enumerate all currently available output devices
        public IEnumerable<IMidiOutputDeviceInfo> OutputDevices {
            get {
                foreach (var rtOutputDeviceInfo in _rtDeviceManager.OutputDevices)
                    yield return new MidiOutputDeviceInfo(rtOutputDeviceInfo);
            }
        }

        // Get the available MIDI API's used by RtMidi (if any)
        public IEnumerable<RtMidiApi> GetAvailableMidiApis()
            => RtMidiManager.GetAvailableApis();

        public void Dispose() {
            if (_disposed) return;

            _rtDeviceManager.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

/**
 * This is a derived work, based on https://github.com/micdah/RtMidi.Core
 * 
 * Copyright (c) 2017 Michael Dahl
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 **/