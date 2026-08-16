using System;
using PeripheralBatteryMonitor.Battery.Core;

namespace PeripheralBatteryMonitor.Battery.Providers
{
    /// <summary>
    /// Reads the battery level Windows itself publishes for a Bluetooth device, from the
    /// association endpoint's property bag -- a 0..100 percentage, available for both BLE and
    /// Classic whenever Windows knows it. It works for devices with no GATT battery service,
    /// which is why it backs up <see cref="BluetoothLEBatteryProvider"/>.
    ///
    /// The property is delivered as null while a device is disconnected, so a null return here
    /// routinely means "asleep" rather than "unsupported".
    /// </summary>
    public class BluetoothBatteryProvider : IBatteryProvider
    {
        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
                //Bluetooth transports only, written as an allowlist: the property bag of a
                //device discovered outside Bluetooth (USB HID) never carries this key, and
                //naming the transports that do apply keeps any future one excluded by default.
            if (ctx.Transport != DeviceTransport.BluetoothLowEnergy &&
                ctx.Transport != DeviceTransport.BluetoothClassic)
                return null;

            object val;
            if (!ctx.TryGetProperty(DeviceProperties.PROP_BATTERY_LEVEL, out val) || val == null)
                return null;
            try
            {
                int value = Convert.ToInt32(val);
                if (value >= 0 && value <= 100)
                    return value;
            }
            catch { }
            return null;
        }
    }
}
