namespace PeripheralBatteryMonitor.Contracts
{
    /// <summary>
    /// A battery source for one family of devices (GATT, a device property, the Apple HID
    /// report, ...). One instance is created per device, so a provider may cache per-device
    /// state (e.g. a GATT connection). Each device binds to the first provider that returns
    /// a value from <see cref="ReadBattery"/> and reuses it thereafter (see <c>BatteryDevice</c>).
    /// </summary>
    public interface IBatteryProvider
    {
        /// <summary>
        /// Current battery level 0..100, or null if this provider can't read THIS device
        /// right now -- whether because it doesn't apply to the device at all or because the
        /// value is momentarily unavailable (the caller keeps the previous value in that
        /// case). May perform I/O and cache whatever it establishes (e.g. GATT connects and
        /// confirms service 0x180F; the Apple provider matches and opens a HID device).
        /// </summary>
        int? ReadBattery(IBatteryDeviceContext ctx);
    }
}
