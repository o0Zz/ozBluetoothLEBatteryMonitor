namespace PeripheralBatteryMonitor
{
    /// <summary>
    /// Callbacks raised by <see cref="DeviceManager"/> as paired devices appear and
    /// disappear. Implemented by the UI layer (the <c>Settings</c> form).
    ///
    /// An interface, but deliberately <b>not</b> in <c>Contracts/</c>. Two reasons: it hands
    /// out a concrete <see cref="BatteryDevice"/>, so filing it under a folder that depends on
    /// nothing would invert the one-way rule the layout rests on; and it is a contract on a
    /// different axis -- Core to the App -- where <c>Contracts/</c> holds the one between
    /// discovery and the battery providers. It belongs here with the other App-facing types.
    /// </summary>
    public interface IDeviceNotification
    {
        void OnNewDevice(BatteryDevice aDevice);
        void OnDeviceRemoved(string deviceId);
    }
}
