using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using BluetoothLEBatteryMonitor.Service.Battery.Core;

namespace BluetoothLEBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// Reads battery from the GATT Battery Service 0x180F / level characteristic 0x2A19.
    /// BLE only; latches off if the service is absent so it's attempted once per device
    /// lifetime. Uses 30 s connect / 5 s read timeouts and caches the connection, so a bound
    /// device doesn't reconnect on every poll.
    /// </summary>
    public class GattBatteryProvider : IBatteryProvider, IDeviceLinkState
    {
        private static readonly Guid BATTERY_UUID = Guid.Parse("{0000180F-0000-1000-8000-00805F9B34FB}");
        private static readonly Guid BATTERY_LEVEL_UUID = Guid.Parse("{00002A19-0000-1000-8000-00805F9B34FB}");

        private const int bleConnectionTimeoutMs = 30000;
        private const int bleReadTimeoutMs = 5000;

        private BluetoothLEDevice   bleDev = null;
        private GattCharacteristic  gattCharacteristic = null;
        private bool                supportGattBattery = true;

        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
            if (ctx.Transport != DeviceTransport.BluetoothLowEnergy || !supportGattBattery)
                return null;
                //Definitive + self-healing: connect and confirm the battery characteristic
                //exists, re-establishing the link if it dropped since we bound.
            if (!IsGattConnected())
                ConnectAndDiscover(ctx);
            if (!IsGattConnected())
                return null;

            Task<GattReadResult> gattReadTask = gattCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask();
            if (gattReadTask.Wait(bleReadTimeoutMs))
            {
                if (GattCommunicationStatus.Success.Equals(gattReadTask.Result.Status))
                {
                    IBuffer buffer = gattReadTask.Result.Value;
                    byte[] data = new byte[buffer.Length];
                    DataReader.FromBuffer(buffer).ReadBytes(data);
                    return data[0];
                }
            }
            return null;
        }

        public bool IsLinkUp(IBatteryDeviceContext ctx)
        {
                //Cheap, no I/O -- just the cached connection status.
            return IsGattConnected();
        }

        private void ConnectAndDiscover(IBatteryDeviceContext ctx)
        {
            bleDev = null;
            gattCharacteristic = null;

            Task<BluetoothLEDevice> bleTask = BluetoothLEDevice.FromIdAsync(ctx.DeviceId).AsTask();
            if (bleTask.Wait(bleConnectionTimeoutMs, new CancellationTokenSource().Token))
            {
                bleDev = bleTask.Result;
                if (bleDev != null)
                    ctx.DeviceName = bleDev.Name;

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

        private bool IsGattConnected()
        {
            return ((gattCharacteristic != null) && (bleDev != null) && (bleDev.ConnectionStatus == BluetoothConnectionStatus.Connected));
        }
    }
}
