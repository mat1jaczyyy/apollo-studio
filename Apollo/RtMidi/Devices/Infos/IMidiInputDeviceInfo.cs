namespace Apollo.RtMidi.Devices.Infos
{
    /// <summary>
    /// Provides information about an available MIDI Input device
    /// </summary>
    public interface IMidiInputDeviceInfo : IMidiDeviceInfo
    {
        /// <summary>
        /// Create MIDI Input device used to receive midi messages for this device
        /// </summary>
        /// <returns>The device.</returns>
        IMidiInputDevice CreateDevice();
    }
}
