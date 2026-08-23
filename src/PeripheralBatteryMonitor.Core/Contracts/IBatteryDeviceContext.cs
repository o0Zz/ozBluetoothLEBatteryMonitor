namespace PeripheralBatteryMonitor.Contracts
{
    /// <summary>
    /// Read-only view of a device that battery providers operate on. Implemented by
    /// <c>BatteryDevice</c> so providers stay decoupled from its internals.
    /// </summary>
    public interface IBatteryDeviceContext
    {
        string DeviceId { get; }
        string DeviceName { get; set; }   //settable: the GATT provider refreshes it from the live device
        DeviceTransport Transport { get; }
        bool TryGetProperty(string key, out object value);
    }
}
