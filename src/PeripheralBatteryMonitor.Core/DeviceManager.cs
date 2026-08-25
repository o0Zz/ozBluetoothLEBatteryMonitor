using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using PeripheralBatteryMonitor.Contracts;
using PeripheralBatteryMonitor.Hid;

namespace PeripheralBatteryMonitor
{
    /// <summary>
    /// Discovery layer. Runs two <see cref="DeviceWatcher"/> instances in
    /// parallel -- one for BLE, one for Bluetooth Classic / BR-EDR -- and maintains the
    /// live set of paired <see cref="BatteryDevice"/>, notifying the UI via
    /// <see cref="IDeviceNotification"/>.
    ///
    /// Not every battery-powered device is a Bluetooth one: a peripheral on its own vendor
    /// dongle has no association endpoint and no pairing, so the watchers never see it.
    /// Those come from <see cref="HidDeviceSource"/> through <see cref="refreshHidDevices"/>,
    /// which feeds the same dictionary. Both sources are otherwise indistinguishable to the
    /// UI -- a device is a device.
    /// </summary>
    public class DeviceManager
    {
        private const string BLE_PROTOCOL_GUID = "{bb7bb05e-5972-42b5-94fc-76eaa7084d49}";
        private const string BREDR_PROTOCOL_GUID = "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";

        private static readonly string[] requestedProperties = new string[]
        {
            "System.Devices.Aep.DeviceAddress",
            "System.Devices.Aep.Bluetooth.Le.IsConnectable",
            DeviceProperties.PROP_AEP_IS_PAIRED,
            DeviceProperties.PROP_AEP_IS_CONNECTED,
            DeviceProperties.PROP_AEP_CONTAINER_ID,
            DeviceProperties.PROP_AEP_CATEGORY,
            DeviceProperties.PROP_BATTERY_LEVEL,
        };

        private ConcurrentDictionary<string, BatteryDevice> deviceDict = new ConcurrentDictionary<string, BatteryDevice>();
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

            watchers.Add(CreateWatcher(BLE_PROTOCOL_GUID, DeviceTransport.BluetoothLowEnergy));
            watchers.Add(CreateWatcher(BREDR_PROTOCOL_GUID, DeviceTransport.BluetoothClassic));

            foreach (DeviceWatcher w in watchers)
                w.Start();

                //No HID enumeration here on purpose: refreshHidDevices reports new devices
                //synchronously, and the UI answers OnNewDevice by running a poll pass -- which
                //would re-enter this method mid-scan. The poll tick is the single driver.
        }

        /// <summary>
        /// Re-snapshot the non-Bluetooth (USB HID) devices and reconcile them into the device
        /// list: newly plugged ones are added, vanished ones removed. There is no watcher to
        /// subscribe to for these, so the caller drives this from the poll tick -- cheap
        /// enough to do every time, and that is what makes unplugging the dongle show up.
        ///
        /// Only touches <see cref="DeviceTransport.UsbHid"/> entries; the Bluetooth ones are
        /// owned by the watchers.
        /// </summary>
        public void refreshHidDevices()
        {
            if (!running)
                return;

            HashSet<string> presentIds = new HashSet<string>();

            foreach (HidInterfaceInfo info in HidDeviceSource.Discover())
            {
                string id = HidDeviceSource.GetDeviceId(info);
                presentIds.Add(id);

                if (deviceDict.ContainsKey(id))
                    continue;

                BatteryDevice device = new BatteryDevice(id,
                    HidDeviceSource.GetDeviceName(info),
                    DeviceTransport.UsbHid,
                    HidDeviceSource.GetProperties(info));

                if (deviceDict.TryAdd(id, device))
                    this.deviceNotification.OnNewDevice(device);
            }

            foreach (KeyValuePair<string, BatteryDevice> kv in deviceDict)
            {
                if (kv.Value.GetTransport() != DeviceTransport.UsbHid)
                    continue;
                if (presentIds.Contains(kv.Key))
                    continue;

                RemoveDevice(kv.Key);
            }
        }

        private DeviceWatcher CreateWatcher(string protocolGuid, DeviceTransport transport)
        {
            string aqsFilter = "(System.Devices.Aep.ProtocolId:=\"" + protocolGuid + "\")";
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqsFilter, requestedProperties, DeviceInformationKind.AssociationEndpoint);

            watcher.Added += (DeviceWatcher deviceWatcher, DeviceInformation devInfo) =>
            {
                if (String.IsNullOrWhiteSpace(devInfo.Name))
                    return;

                if (!devInfo.Pairing.IsPaired)
                    return;

                if (deviceDict.ContainsKey(devInfo.Id))
                    return;

                BatteryDevice device = new BatteryDevice(devInfo, transport);
                if (deviceDict.TryAdd(devInfo.Id, device))
                {
                    ReconcilePhoneNames(device);
                    this.deviceNotification.OnNewDevice(device);
                }
            };

            watcher.Updated += (DeviceWatcher deviceWatcher, DeviceInformationUpdate devUpdate) =>
            {
                if (devUpdate.Properties != null)
                {
                        //An IsPaired flip to false won't fire Removed, only Updated. Re-check pairing.
                    object isPaired;
                    if (devUpdate.Properties.TryGetValue(DeviceProperties.PROP_AEP_IS_PAIRED, out isPaired)
                        && isPaired is bool
                        && !(bool)isPaired)
                    {
                        RemoveDevice(devUpdate.Id);
                        return;
                    }

                        //Forward fresh property values into the cached BatteryDevice so the property-based
                        //battery strategies see updates without a full re-enumeration.
                    BatteryDevice existing;
                    if (deviceDict.TryGetValue(devUpdate.Id, out existing))
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

        /// <summary>
        /// Windows can expose one phone through both Bluetooth transports under the same AEP
        /// container. On iOS the BLE endpoint may use an opaque local name while the Classic
        /// endpoint carries the useful device name. Once both have appeared, use the Classic
        /// sibling's name for the BLE endpoint too.
        ///
        /// Container and phone category establish that the endpoints belong to the same
        /// physical phone; no device-name pattern or Apple-specific value is assumed.
        /// </summary>
        private void ReconcilePhoneNames(BatteryDevice added)
        {
            Guid containerId;
            if (!TryGetContainerId(added, out containerId) || !IsPhone(added))
                return;

            string classicName = null;
            foreach (BatteryDevice candidate in deviceDict.Values)
            {
                Guid candidateContainer;
                if (!TryGetContainerId(candidate, out candidateContainer) ||
                    candidateContainer != containerId || !IsPhone(candidate))
                    continue;

                if (candidate.GetTransport() == DeviceTransport.BluetoothClassic &&
                    !String.IsNullOrWhiteSpace(candidate.GetName()))
                {
                    classicName = candidate.GetName();
                    break;
                }
            }

            if (classicName == null)
                return;

            foreach (BatteryDevice candidate in deviceDict.Values)
            {
                Guid candidateContainer;
                if (!TryGetContainerId(candidate, out candidateContainer) ||
                    candidateContainer != containerId || !IsPhone(candidate))
                    continue;

                if (candidate.GetTransport() == DeviceTransport.BluetoothLowEnergy)
                    candidate.UpdateName(classicName);
            }
        }

        private static bool TryGetContainerId(BatteryDevice device, out Guid containerId)
        {
            object value;
            if (device.TryGetProperty(DeviceProperties.PROP_AEP_CONTAINER_ID, out value))
            {
                if (value is Guid)
                {
                    containerId = (Guid)value;
                    return containerId != Guid.Empty;
                }

                if (value != null && Guid.TryParse(value.ToString(), out containerId))
                    return containerId != Guid.Empty;
            }

            containerId = Guid.Empty;
            return false;
        }

        private static bool IsPhone(BatteryDevice device)
        {
            object value;
            if (!device.TryGetProperty(DeviceProperties.PROP_AEP_CATEGORY, out value))
                return false;

            string[] categories = value as string[];
            if (categories == null)
                return false;

            foreach (string category in categories)
            {
                if (String.Equals(category, "Communication.Phone", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void RemoveDevice(string id)
        {
            BatteryDevice removed;
            if (deviceDict.TryRemove(id, out removed))
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

        public ConcurrentDictionary<string, BatteryDevice> getDeviceList()
        {
            return deviceDict;
        }
    }
}
