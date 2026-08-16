using System;
using System.Collections.Generic;
using PeripheralBatteryMonitor.Battery.Core;
using PeripheralBatteryMonitor.Battery.Providers.Logitech;

namespace PeripheralBatteryMonitor.Battery.Providers
{
    /// <summary>
    /// Ordered registry of battery providers. Registration order is priority order: when a
    /// device is first polled, providers are probed top-down and it binds to the first whose
    /// <see cref="IBatteryProvider.ReadBattery"/> returns a value. The four built-ins are
    /// registered by the static constructor; callers may append their own via
    /// <see cref="Register"/> before the first device is created.
    /// </summary>
    public static class BatteryProviderRegistry
    {
        private static readonly List<Func<IBatteryProvider>> factories = new List<Func<IBatteryProvider>>();

        static BatteryProviderRegistry()
        {
                //Priority order. Each provider rejects the transports it doesn't serve, so this
                //ordering only decides which one wins where several could answer.
            Register(() => new BluetoothLEBatteryProvider());     //1. GATT 0x180F (BLE only)
            Register(() => new BluetoothBatteryProvider());       //2. Battery level published by Windows
            Register(() => new AppleBatteryProvider());           //3. Apple Magic HID report 0x90
            Register(() => new LogitechBatteryProvider());        //4. Logitech HID++ (USB HID only)
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
