namespace Apollo.RtMidi.Devices.Infos
{
    /// <summary>
    /// Provides information about an available MIDI Output device
    /// </summary>
    public interface IMidiOutputDeviceInfo : IMidiDeviceInfo
    {
        /// <summary>
        /// Create MIDI Output device used to send midi messages to this device
        /// </summary>
        /// <returns>The device.</returns>
        IMidiOutputDevice CreateDevice();
    }
}
