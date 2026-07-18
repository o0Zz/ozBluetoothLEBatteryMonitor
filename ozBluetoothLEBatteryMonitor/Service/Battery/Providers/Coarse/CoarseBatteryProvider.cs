using System;
using BluetoothLEBatteryMonitor.Service.Battery.Core;

namespace BluetoothLEBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// Reads the coarse System.Devices.BatteryLife enum, mapped to representative bucket
    /// percentages. Last resort -- a rough band, not an exact level.
    /// </summary>
    public class CoarseBatteryProvider : IBatteryProvider
    {
        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
            object val;
            if (!ctx.TryGetProperty(DeviceProperties.PROP_BATTERY_LIFE, out val) || val == null)
                return null;
            try
            {
                int life = Convert.ToInt32(val);
                switch (life)
                {
                    case 1: return 10;  //Critical
                    case 2: return 30;  //Low
                    case 3: return 60;  //Average
                    case 4: return 90;  //Full
                }
            }
            catch { }
            return null;
        }
    }
}
