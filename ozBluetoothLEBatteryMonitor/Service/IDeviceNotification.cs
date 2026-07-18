namespace BluetoothLEBatteryMonitor.Service
{
    /// <summary>
    /// Callbacks raised by <see cref="DeviceManager"/> as paired devices appear and
    /// disappear. Implemented by the UI layer (the <c>Settings</c> form).
    /// </summary>
    public interface IDeviceNotification
    {
        void OnNewDevice(BatteryDevice aDevice);
        void OnDeviceRemoved(string deviceId);
    }
}
