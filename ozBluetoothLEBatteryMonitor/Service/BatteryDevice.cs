using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using BluetoothLEBatteryMonitor.Service.Battery.Core;
using BluetoothLEBatteryMonitor.Service.Battery.Providers;

namespace BluetoothLEBatteryMonitor.Service
{
    /// <summary>
    /// One tracked device's battery state. A thin state holder: property caching plus the
    /// battery provider bound to this device. Once a provider reads the device successfully
    /// it's remembered (<see cref="boundProvider"/>) and read directly on later polls as a
    /// fast path; if it ever comes up empty we fall back to probing the other providers, so
    /// a transient failure never leaves the device stuck. Implements
    /// <see cref="IBatteryDeviceContext"/> so providers read what they need without touching
    /// these internals.
    /// </summary>
    public class BatteryDevice : IBatteryDeviceContext
    {
        private string              deviceID = "";
        private string              deviceName = "";
        private int                 batteryLevel = -1;
        private DeviceTransport     transport = DeviceTransport.BluetoothLowEnergy;
        private DateTime            lastUpdatedTime;
            //Did the most recent poll actually get a value out of the device? For transports
            //with no OS-level connection state to consult (USB HID), answering the device
            //*is* the liveness test.
        private bool                lastReadSucceeded = false;
        private ConcurrentDictionary<string, object> propertyCache = new ConcurrentDictionary<string, object>();

            //Candidate providers for this device, in priority order. One list per device so a
            //stateful provider (e.g. GATT) can cache its connection. See Service/Battery/.
        private readonly List<IBatteryProvider> providers = BatteryProviderRegistry.CreateProviders();

            //The provider that last read this device (a fast-path hint, not a hard lock).
        private IBatteryProvider boundProvider = null;

        public BatteryDevice(DeviceInformation deviceInfo, DeviceTransport transport)
        {
            this.deviceID = deviceInfo.Id;
            this.deviceName = deviceInfo.Name;
            this.transport = transport;
            CacheProperties(deviceInfo.Properties);
            UpdateBatteryLevel();
        }

        /// <summary>
        /// For devices that reach the PC outside Bluetooth and so have no
        /// <see cref="DeviceInformation"/> behind them (a peripheral on its own USB dongle,
        /// found by <see cref="HidDeviceSource"/>). The property bag is seeded by the caller
        /// with whatever the bound provider needs to reach the device again.
        /// </summary>
        public BatteryDevice(string deviceId, string deviceName, DeviceTransport transport, IReadOnlyDictionary<string, object> properties)
        {
            this.deviceID = deviceId;
            this.deviceName = deviceName;
            this.transport = transport;
            if (properties != null)
                CacheProperties(properties);
            UpdateBatteryLevel();
        }

        public void UpdateProperties(IReadOnlyDictionary<string, object> updated)
        {
            if (updated == null) return;
            CacheProperties(updated);
        }

        private void CacheProperties(IReadOnlyDictionary<string, object> source)
        {
            foreach (KeyValuePair<string, object> kv in source)
            {
                if (kv.Value != null)
                    propertyCache[kv.Key] = kv.Value;
            }
        }

        public void UpdateBatteryLevel()
        {
            lastUpdatedTime = DateTime.Now;

                //Fast path: the provider that read this device last time is usually still the
                //right one, so read it directly without re-probing everything.
            if (boundProvider != null)
            {
                int? level = boundProvider.ReadBattery(this);
                if (level.HasValue)
                {
                    batteryLevel = level.Value;
                    lastReadSucceeded = true;
                    return;
                }
            }

                //(Re)resolve: probe providers in priority order and take the first that yields
                //a reading. Falling back like this (instead of staying stuck on a provider
                //that went quiet) preserves the original behaviour of trying every source
                //until one produces a value. A null reading means "can't read this device
                //right now", so it doubles as the capability check.
            foreach (IBatteryProvider provider in providers)
            {
                if (provider == boundProvider)
                    continue;   //already attempted on the fast path above

                int? level = provider.ReadBattery(this);
                if (level.HasValue)
                {
                    batteryLevel = level.Value;
                    boundProvider = provider;
                    lastReadSucceeded = true;
                    Debug.WriteLine("[Battery] '" + deviceName + "' <- " + provider.GetType().Name + " = " + level.Value + "%");
                    return;
                }
            }

                //Nothing produced a value this tick -> keep the last known level (-1 = never read).
            lastReadSucceeded = false;
            Debug.WriteLine("[Battery] '" + deviceName + "' <- no provider produced a value (transport=" + transport + ", level=" + batteryLevel + ")");
        }

        public bool IsConnected()
        {
                //USB HID: no OS connection state exists for the device behind the dongle, so
                //"did it answer the last poll" is the only meaningful answer. The dongle can
                //stay plugged in with the headset switched off.
            if (transport == DeviceTransport.UsbHid)
                return lastReadSucceeded;

            if (transport == DeviceTransport.BluetoothClassic)
            {
                object aepConnected;
                if (propertyCache.TryGetValue(DeviceProperties.PROP_AEP_IS_CONNECTED, out aepConnected) && aepConnected is bool)
                    return (bool)aepConnected;
                return batteryLevel >= 0;
            }

                //BLE: defer to the first provider that tracks a live link (the GATT provider).
            foreach (IBatteryProvider provider in providers)
            {
                IDeviceLinkState link = provider as IDeviceLinkState;
                if (link != null)
                    return link.IsLinkUp(this);
            }
            return batteryLevel >= 0;
        }

        public int GetBatteryLevel()
        {
            return batteryLevel;
        }

        public string GetName()
        {
            return deviceName;
        }

        public DeviceTransport GetTransport()
        {
            return transport;
        }

        public DateTime GetLastUpdatedTime()
        {
            return lastUpdatedTime;
        }

        /* ---- IBatteryDeviceContext (data providers read/write) ---- */

        string IBatteryDeviceContext.DeviceId { get { return deviceID; } }

        string IBatteryDeviceContext.DeviceName
        {
            get { return deviceName; }
            set { deviceName = value; }
        }

        DeviceTransport IBatteryDeviceContext.Transport { get { return transport; } }

        bool IBatteryDeviceContext.TryGetProperty(string key, out object value)
        {
            return propertyCache.TryGetValue(key, out value);
        }
    }
}
