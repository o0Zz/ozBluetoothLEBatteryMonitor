using System;
using BluetoothLEBatteryMonitor.Service.Battery.Core;

namespace BluetoothLEBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// Reads the AEP DEVPKEY_Device_BatteryLevel property -- a precise 0..100 byte surfaced
    /// in the device property bag when Windows knows it.
    /// </summary>
    public class DevicePropertyBatteryProvider : IBatteryProvider
    {
        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
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
