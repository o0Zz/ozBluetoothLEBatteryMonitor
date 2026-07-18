using System;
using System.Collections.Generic;
using BluetoothLEBatteryMonitor.Service.Battery.Core;

namespace BluetoothLEBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// Ordered registry of battery providers. Registration order is priority order: when a
    /// device is first polled, providers are probed top-down and it binds to the first that
    /// <see cref="IBatteryProvider.Supports"/> it. The built-ins are registered by the
    /// static constructor; callers may append their own via <see cref="Register"/> before
    /// the first device is created.
    /// </summary>
    public static class BatteryProviderRegistry
    {
        private static readonly List<Func<IBatteryProvider>> factories = new List<Func<IBatteryProvider>>();

        static BatteryProviderRegistry()
        {
                //Priority order: precise first, coarse last.
            Register(() => new GattBatteryProvider());            //1. GATT 0x180F (BLE only)
            Register(() => new DevicePropertyBatteryProvider());  //2. DEVPKEY_Device_BatteryLevel
            Register(() => new AppleBatteryProvider());           //3. Apple Magic HID report 0x90
            Register(() => new CoarseBatteryProvider());          //4. System.Devices.BatteryLife
        }

        /// <summary>Append a provider factory. Later registrations have lower priority.</summary>
        public static void Register(Func<IBatteryProvider> factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            factories.Add(factory);
        }

        /// <summary>Create a fresh provider list for one device, in priority order.</summary>
        public static List<IBatteryProvider> CreateProviders()
        {
            List<IBatteryProvider> list = new List<IBatteryProvider>(factories.Count);
            foreach (Func<IBatteryProvider> factory in factories)
                list.Add(factory());
            return list;
        }
    }
}
