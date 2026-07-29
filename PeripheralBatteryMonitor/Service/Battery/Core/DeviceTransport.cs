namespace PeripheralBatteryMonitor.Service.Battery.Core
{
    /// <summary>
    /// How a tracked device is reached. Battery providers consult this to decide whether
    /// they apply (e.g. GATT exists only over <see cref="BluetoothLowEnergy"/>).
    /// </summary>
    public enum DeviceTransport
    {
        BluetoothLowEnergy,  //Bluetooth LE / BLE
        BluetoothClassic,   //Bluetooth Classic / BR-EDR
        UsbHid,             //Raw USB HID, e.g. a device on its own vendor dongle (no Bluetooth at all)
    }
}
