using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothLEBatteryMonitor.Service
{
    /* --------------------------------------------------------------------- */

    public interface IDeviceNotification
    {
        void OnNewDevice(DeviceBLE aDevice);
        void OnDeviceRemoved(string deviceId);
    }

    /* --------------------------------------------------------------------- */

    public class DeviceBLE
    {
        public static readonly Guid BATTERY_UUID = Guid.Parse("{0000180F-0000-1000-8000-00805F9B34FB}");
        public static readonly Guid BATTERY_LEVEL_UUID = Guid.Parse("{00002A19-0000-1000-8000-00805F9B34FB}");

            //DEVPROPKEY string-form: "{guid} pid". DEVPKEY_Device_BatteryLevel reports a byte 0..100.
        public const string PROP_BATTERY_LEVEL = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
            //Coarse battery enum (Critical/Low/Average/Full).
        public const string PROP_BATTERY_LIFE = "System.Devices.BatteryLife";
        public const string PROP_AEP_IS_CONNECTED = "System.Devices.Aep.IsConnected";
        public const string PROP_AEP_IS_PAIRED = "System.Devices.Aep.IsPaired";

        private int bleConnectionTimeoutMs = 30000;
        private int bleReadTimeoutMs = 5000;

        private BluetoothLEDevice   bleDev = null;
        private GattCharacteristic  gattCharacteristic = null;
        private string              deviceID = "";
        private string              deviceName = "";
        private int                 batteryLevel = -1;
        private bool                supportGattBattery = true;
        private bool                isClassic = false;
        private DateTime            lastUpdatedTime;
        private ConcurrentDictionary<string, object> propertyCache = new ConcurrentDictionary<string, object>();

        public DeviceBLE(DeviceInformation deviceInfo, bool isClassic)
        {
            this.deviceID = deviceInfo.Id;
            this.deviceName = deviceInfo.Name;
            this.isClassic = isClassic;
            CacheProperties(deviceInfo.Properties);
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

        private void ConnectAndDiscover()
        {
            bleDev = null;
            gattCharacteristic = null;

            Task<BluetoothLEDevice> bleTask = BluetoothLEDevice.FromIdAsync(deviceID).AsTask();
            if (bleTask.Wait(bleConnectionTimeoutMs, new CancellationTokenSource().Token))
            {
                bleDev = bleTask.Result;
                if (bleDev != null)
                    deviceName = bleDev.Name;

                Task<GattDeviceServicesResult> batteryServiceTask = bleDev.GetGattServicesForUuidAsync(BATTERY_UUID, BluetoothCacheMode.Uncached).AsTask();
                if (batteryServiceTask.Wait(bleReadTimeoutMs))
                {
                    if (GattCommunicationStatus.Success.Equals(batteryServiceTask.Result.Status))
                    {
                        if (batteryServiceTask.Result.Services == null || batteryServiceTask.Result.Services.Count == 0)
                        {
                                //GATT 0x180F not present -- skip GATT for the rest of this device's lifetime
                            supportGattBattery = false;
                        }
                        else
                        {
                            Task<GattCharacteristicsResult> gattCharacteristicsTask = batteryServiceTask.Result.Services[0].GetCharacteristicsForUuidAsync(BATTERY_LEVEL_UUID, BluetoothCacheMode.Uncached).AsTask();
                            if (gattCharacteristicsTask.Wait(bleReadTimeoutMs))
                            {
                                if (GattCommunicationStatus.Success.Equals(gattCharacteristicsTask.Result.Status)
                                    && gattCharacteristicsTask.Result.Characteristics != null
                                    && gattCharacteristicsTask.Result.Characteristics.Count > 0)
                                {
                                    gattCharacteristic = gattCharacteristicsTask.Result.Characteristics[0];
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateBatteryLevel()
        {
            lastUpdatedTime = DateTime.Now;

                //Strategy 1: GATT Battery Service (BLE only, until proven unsupported)
            if (!isClassic && supportGattBattery)
                if (TryReadFromGatt()) return;

                //Strategy 2: AEP DEVPKEY_Device_BatteryLevel (precise byte 0..100)
            if (TryReadFromDeviceProperty()) return;

                //Strategy 3: bucketed System.Devices.BatteryLife enum
            if (TryReadFromCoarseEnum()) return;

                //None succeeded -- keep last known value
        }

        private bool TryReadFromGatt()
        {
            if (!IsGattConnected())
                ConnectAndDiscover();

            if (!IsGattConnected()) return false;

            Task<GattReadResult> gattReadTask = gattCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask();
            if (gattReadTask.Wait(bleReadTimeoutMs))
            {
                if (GattCommunicationStatus.Success.Equals(gattReadTask.Result.Status))
                {
                    IBuffer buffer = gattReadTask.Result.Value;
                    byte[] data = new byte[buffer.Length];
                    DataReader.FromBuffer(buffer).ReadBytes(data);
                    batteryLevel = data[0];
                    return true;
                }
            }
            return false;
        }

        private bool TryReadFromDeviceProperty()
        {
            object val;
            if (!propertyCache.TryGetValue(PROP_BATTERY_LEVEL, out val) || val == null)
                return false;
            try
            {
                int level = Convert.ToInt32(val);
                if (level >= 0 && level <= 100)
                {
                    batteryLevel = level;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private bool TryReadFromCoarseEnum()
        {
            object val;
            if (!propertyCache.TryGetValue(PROP_BATTERY_LIFE, out val) || val == null)
                return false;
            try
            {
                int life = Convert.ToInt32(val);
                switch (life)
                {
                    case 1: batteryLevel = 10; return true;  //Critical
                    case 2: batteryLevel = 30; return true;  //Low
                    case 3: batteryLevel = 60; return true;  //Average
                    case 4: batteryLevel = 90; return true;  //Full
                }
            }
            catch { }
            return false;
        }

        private bool IsGattConnected()
        {
            return ((gattCharacteristic != null) && (bleDev != null) && (bleDev.ConnectionStatus == BluetoothConnectionStatus.Connected));
        }

        public bool IsConnected()
        {
            if (isClassic)
            {
                object aepConnected;
                if (propertyCache.TryGetValue(PROP_AEP_IS_CONNECTED, out aepConnected) && aepConnected is bool)
                    return (bool)aepConnected;
                return batteryLevel >= 0;
            }
            return IsGattConnected();
        }

        public int GetBatteryLevel()
        {
            return batteryLevel;
        }

        public string GetName()
        {
            return deviceName;
        }

        public DateTime GetLastUpdatedTime()
        {
            return lastUpdatedTime;
        }
    }

    /* --------------------------------------------------------------------- */

    public class DeviceManager
    {
        private const string BLE_PROTOCOL_GUID = "{bb7bb05e-5972-42b5-94fc-76eaa7084d49}";
        private const string BREDR_PROTOCOL_GUID = "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";

        private static readonly string[] requestedProperties = new string[]
        {
            "System.Devices.Aep.DeviceAddress",
            "System.Devices.Aep.Bluetooth.Le.IsConnectable",
            DeviceBLE.PROP_AEP_IS_PAIRED,
            DeviceBLE.PROP_AEP_IS_CONNECTED,
            DeviceBLE.PROP_BATTERY_LEVEL,
            DeviceBLE.PROP_BATTERY_LIFE,
        };

        private ConcurrentDictionary<string, DeviceBLE> deviceBLEDict = new ConcurrentDictionary<string, DeviceBLE>();
        private List<DeviceWatcher> watchers = new List<DeviceWatcher>();
        private IDeviceNotification deviceNotification;
        private bool running = false;
        private bool scanForEver = false;

        public DeviceManager(IDeviceNotification deviceNotification)
        {
            this.deviceNotification = deviceNotification;
        }

        public void scan(bool scanForEver = false)
        {
            if (running == true)
                return; //Scan already in progress ...

            running = true;
            this.scanForEver = scanForEver;

            watchers.Add(CreateWatcher(BLE_PROTOCOL_GUID, false));
            watchers.Add(CreateWatcher(BREDR_PROTOCOL_GUID, true));

            foreach (DeviceWatcher w in watchers)
                w.Start();
        }

        private DeviceWatcher CreateWatcher(string protocolGuid, bool isClassic)
        {
            string aqsFilter = "(System.Devices.Aep.ProtocolId:=\"" + protocolGuid + "\")";
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqsFilter, requestedProperties, DeviceInformationKind.AssociationEndpoint);

            watcher.Added += (DeviceWatcher deviceWatcher, DeviceInformation devInfo) =>
            {
                if (String.IsNullOrWhiteSpace(devInfo.Name))
                    return;

                if (!devInfo.Pairing.IsPaired)
                    return;

                if (deviceBLEDict.ContainsKey(devInfo.Id))
                    return;

                DeviceBLE deviceBLE = new DeviceBLE(devInfo, isClassic);
                deviceBLEDict.TryAdd(devInfo.Id, deviceBLE);
                this.deviceNotification.OnNewDevice(deviceBLE);
            };

            watcher.Updated += (DeviceWatcher deviceWatcher, DeviceInformationUpdate devUpdate) =>
            {
                if (devUpdate.Properties != null)
                {
                        //An IsPaired flip to false won't fire Removed, only Updated. Re-check pairing.
                    object isPaired;
                    if (devUpdate.Properties.TryGetValue(DeviceBLE.PROP_AEP_IS_PAIRED, out isPaired)
                        && isPaired is bool
                        && !(bool)isPaired)
                    {
                        RemoveDevice(devUpdate.Id);
                        return;
                    }

                        //Forward fresh property values into the cached DeviceBLE so the property-based
                        //battery strategies see updates without a full re-enumeration.
                    DeviceBLE existing;
                    if (deviceBLEDict.TryGetValue(devUpdate.Id, out existing))
                        existing.UpdateProperties(devUpdate.Properties);
                }
            };

            watcher.Removed += (DeviceWatcher deviceWatcher, DeviceInformationUpdate devUpdate) =>
            {
                RemoveDevice(devUpdate.Id);
            };

            watcher.EnumerationCompleted += (DeviceWatcher deviceWatcher, object arg) =>
            {
                deviceWatcher.Stop();
            };

            watcher.Stopped += (DeviceWatcher deviceWatcher, object arg) =>
            {
                if (running && this.scanForEver)
                    deviceWatcher.Start();
            };

            return watcher;
        }

        private void RemoveDevice(string id)
        {
            DeviceBLE removed;
            if (deviceBLEDict.TryRemove(id, out removed))
                this.deviceNotification.OnDeviceRemoved(id);
        }

        public void stopScan()
        {
            running = false;
            foreach (DeviceWatcher w in watchers)
            {
                try { w.Stop(); } catch { /* already stopped */ }
            }
            watchers.Clear();
        }

        public ConcurrentDictionary<string, DeviceBLE> getDeviceList()
        {
            return deviceBLEDict;
        }
    }
}
