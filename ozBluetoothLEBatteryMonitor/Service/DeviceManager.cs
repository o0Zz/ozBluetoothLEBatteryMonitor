using System;
using System.Collections.Concurrent;
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

        private int bleConnectionTimeoutMs = 30000;
        private int bleReadTimeoutMs = 5000;

        private BluetoothLEDevice   bleDev = null;
        private GattCharacteristic  gattCharacteristic = null;
        private string              deviceID = "";
        private string              deviceName = "";
        private int                 batteryLevel = -1;
        private bool                supportBatterylevel = true;
        private DateTime            lastUpdatedTime;
        public DeviceBLE(DeviceInformation deviceInfo)
        {
            deviceID = deviceInfo.Id;
            deviceName = deviceInfo.Name;

            UpdateBatteryLevel();
        }

        private void ConnectAndDiscover()
        {
            bleDev = null;
            gattCharacteristic = null;

            Task<BluetoothLEDevice> bleTask = BluetoothLEDevice.FromIdAsync(deviceID).AsTask(); //Connect to device
            if (bleTask.Wait(bleConnectionTimeoutMs, new CancellationTokenSource().Token)) 
            {
                bleDev = bleTask.Result;

                //Update device name 
                deviceName = bleDev.Name;

                Task<GattDeviceServicesResult> batteryServiceTask = bleDev.GetGattServicesForUuidAsync(BATTERY_UUID, BluetoothCacheMode.Uncached).AsTask(); //Discover device services
                if (batteryServiceTask.Wait(bleReadTimeoutMs))
                {
                    if (GattCommunicationStatus.Success.Equals(batteryServiceTask.Result.Status))
                    {
                        if (batteryServiceTask.Result.Services == null || batteryServiceTask.Result.Services.Count == 0)
                        {
                                //Service battery not available on this device (No need to update battery)
                            supportBatterylevel = false;
                        }
                        else
                        {
                            Task<GattCharacteristicsResult> gattCharacteristicsTask = batteryServiceTask.Result.Services[0].GetCharacteristicsForUuidAsync(BATTERY_LEVEL_UUID, BluetoothCacheMode.Uncached).AsTask();
                            if (gattCharacteristicsTask.Wait(bleReadTimeoutMs)) //Get battery characteristic
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
            if (!supportBatterylevel)
                return;

            if (!IsConnected())
                ConnectAndDiscover();

            if (IsConnected())
            {
                Task<GattReadResult> gattReadTask = gattCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask();
                if (gattReadTask.Wait(bleReadTimeoutMs))
                {
                    if (GattCommunicationStatus.Success.Equals(gattReadTask.Result.Status))
                    {
                        IBuffer buffer = gattReadTask.Result.Value;
                        byte[] data = new byte[buffer.Length];
                        DataReader.FromBuffer(buffer).ReadBytes(data);
                        batteryLevel = data[0];
                    }
                }
            }

            lastUpdatedTime = DateTime.Now;
        }

        public bool IsConnected()
        {
            return ((gattCharacteristic != null) && (bleDev != null) && (bleDev.ConnectionStatus == BluetoothConnectionStatus.Connected));
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
        private ConcurrentDictionary<string, DeviceBLE> deviceBLEDict = new ConcurrentDictionary<string, DeviceBLE>();
        private DeviceWatcher watcher = null;
        IDeviceNotification deviceNotification;
        private bool running = false;

        public DeviceManager(IDeviceNotification deviceNotification)
        {
            this.deviceNotification = deviceNotification;
        }

        public void scan(bool scanForEver = false)
        {
            if (running == true)
                return; //Scan already in progress ...

            running = true;

            string aqsFilter = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            string[] bleAdditionalProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.Bluetooth.Le.IsConnectable", };
            watcher = DeviceInformation.CreateWatcher(aqsFilter, bleAdditionalProperties, DeviceInformationKind.AssociationEndpoint);
            watcher.Added += (DeviceWatcher deviceWatcher, DeviceInformation devInfo) =>
            {
                if (!String.IsNullOrWhiteSpace(devInfo.Name))
                {
                    if (!devInfo.Pairing.IsPaired)
                        return;

                    if ( !deviceBLEDict.ContainsKey(devInfo.Id) )
                    {
                        DeviceBLE deviceBLE = new DeviceBLE(devInfo);
                        deviceBLEDict.TryAdd(devInfo.Id, deviceBLE);

                        this.deviceNotification.OnNewDevice(deviceBLE);
                    } 
                }
            };
            watcher.Updated += (DeviceWatcher deviceWatcher, DeviceInformationUpdate devUpdate) =>
            {
                    //An IsPaired flip to false won't fire Removed, only Updated. Re-check pairing.
                object isPaired;
                if (devUpdate.Properties != null
                    && devUpdate.Properties.TryGetValue("System.Devices.Aep.IsPaired", out isPaired)
                    && isPaired is bool
                    && !(bool)isPaired)
                {
                    RemoveDevice(devUpdate.Id);
                }
            };
            watcher.Removed += (DeviceWatcher deviceWatcher, DeviceInformationUpdate devUpdate) =>
            {
                RemoveDevice(devUpdate.Id);
            };
            watcher.EnumerationCompleted += (DeviceWatcher deviceWatcher, object arg) => {
                deviceWatcher.Stop();
            };
            watcher.Stopped += (DeviceWatcher deviceWatcher, object arg) => {
                if (running && scanForEver)
                {
                    deviceWatcher.Start();
                    return;
                }

                running = false;
                watcher = null;
            };
            watcher.Start();
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
            if (watcher != null)
                watcher.Stop();
        }

        public ConcurrentDictionary<string, DeviceBLE> getDeviceList()
        {
            return deviceBLEDict;
        }
    }
}
