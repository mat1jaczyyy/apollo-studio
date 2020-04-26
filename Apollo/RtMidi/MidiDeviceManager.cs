using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Apollo.RtMidi.Interface;
using Apollo.RtMidi.Devices.Infos;
using Apollo.RtMidi.Interface.API;
using System;

namespace Apollo.RtMidi
{
    /// <summary>
    /// This is the MIDI Device Manager, which you shall use to obtain information
    /// about all available input and output devices, information is always up-to-date
    /// so each time you enumerate input or output devices, it will reflect the 
    /// currently available devices.
    /// </summary>
    public sealed class MidiDeviceManager : IDisposable
    {
        /// <summary>
        /// Manager singleton instance to use
        /// </summary>
        public static MidiDeviceManager Default => DefaultHolder.Value;

        private static readonly Lazy<MidiDeviceManager> DefaultHolder = new Lazy<MidiDeviceManager>(() => new MidiDeviceManager());

        private readonly Interface.RtMidiManager _rtDeviceManager;
        private bool _disposed;

        private MidiDeviceManager()
        {
            _rtDeviceManager = Interface.RtMidiManager.Default;
        }

        ~MidiDeviceManager() 
        {
            Dispose();
        }

        /// <summary>
        /// Enumerate all currently available input devices
        /// </summary>
        public IEnumerable<IMidiInputDeviceInfo> InputDevices 
        {
            get
            {
                foreach (var rtInputDeviceInfo in _rtDeviceManager.InputDevices) 
                {
                    yield return new MidiInputDeviceInfo(rtInputDeviceInfo);
                }
            }
        }

        /// <summary>
        /// Enumerate all currently available output devices
        /// </summary>
        public IEnumerable<IMidiOutputDeviceInfo> OutputDevices
        {
            get 
            {
                foreach (var rtOutputDeviceInfo in _rtDeviceManager.OutputDevices) 
                {
                    yield return new MidiOutputDeviceInfo(rtOutputDeviceInfo);
                }
            }
        }

        /// <summary>
        /// Get the available MIDI API's used by RtMidi (if any)
        /// </summary>
        /// <returns>The available midi apis.</returns>
        public IEnumerable<RtMidiApi> GetAvailableMidiApis()
        {
            return RtMidiManager.GetAvailableApis();
        }

        public void Dispose()
        {
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