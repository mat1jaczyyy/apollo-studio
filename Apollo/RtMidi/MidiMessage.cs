using Apollo.RtMidi.Devices;
using System;
using System.Linq;

namespace Apollo.RtMidi
{
    public readonly struct MidiMessage
    {
        public readonly byte[] Data;

        public MidiMessage(byte[] data)
        {
            Data = data;
        }

        public byte Status => Data[0];
        public byte End => Data[^1];

        public byte Type => (byte)(Data[0] & 0xF0);
        public byte Channel => (byte)(Data[0] & 0x0F);

        public byte Pitch => Data[1];
        public byte Velocity => Data[2];

        public bool IsSysEx => Status == 0xF0 && End == 0xF7;
        public bool IsNote => (Status & 0xE0) == 0x80;
        public bool IsCC => Type == 0xB0;

        public bool IsNoteOn => Type == 0x90;
        public bool IsNoteOff => Type == 0x80;

        public bool CheckSysExHeader(byte[] header)
            => IsSysEx
                ? header.SequenceEqual(Data.Skip(1).Take(header.Length))
                : false;

        public override string ToString()
        {
            return string.Join(" ", Data);
        }
    }
}