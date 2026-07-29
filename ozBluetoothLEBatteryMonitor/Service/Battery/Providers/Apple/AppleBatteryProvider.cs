using System;
using System.Collections.Generic;
using System.Text;
using BluetoothLEBatteryMonitor.Service.Battery.Core;
using BluetoothLEBatteryMonitor.Service.Battery.Hid;

namespace BluetoothLEBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// Reads battery for Apple "Magic" devices (Magic Mouse / Magic Trackpad / Magic
    /// Keyboard). These do NOT expose battery through the Bluetooth property bag
    /// (<see cref="DeviceProperties.PROP_BATTERY_LEVEL"/>), so the other providers
    /// never see them. Instead they report battery through a vendor HID input report
    /// (report id 0x90), where byte[2] holds the level as a plain 0..100 percentage -- only
    /// reachable through the raw HID stack, which this provider drives via
    /// <see cref="HidInterfaceEnumerator"/> / <see cref="HidDevice"/>. Verified against the
    /// Linux hid-magicmouse driver and the WinMagicBattery Windows implementation.
    ///
    /// Note these devices are still discovered over Bluetooth like any other paired device --
    /// only the battery *reading* goes over HID. So unlike the Logitech provider, this one
    /// registers no <see cref="HidDeviceSpec"/>; doing so would list the device twice.
    ///
    /// <see cref="ReadBattery"/> is definitive: it yields a value only when a matching Apple
    /// HID battery is actually found for this device.
    /// </summary>
    public class AppleBatteryProvider : IBatteryProvider
    {
            //Apple vendor id, as reported by HidD_GetAttributes: 0x004C over Bluetooth,
            //0x05AC over USB. (The device path spells the BT one "vid&0002004c", which is why
            //the enumerator pre-filters paths on the bare hex digits rather than "vid_".)
        private const ushort APPLE_VID_BT = 0x004C;
        private const ushort APPLE_VID_USB = 0x05AC;

        private static readonly ushort[] appleVendorIds = new ushort[] { APPLE_VID_BT, APPLE_VID_USB };

        private const byte BATTERY_REPORT_ID = 0x90;   //Apple vendor battery input report

            //A GET_REPORT right after the device wakes can fail or answer with stale data.
        private const int READ_ATTEMPTS = 3;

        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
            object addr;
            ctx.TryGetProperty(DeviceProperties.PROP_AEP_DEVICE_ADDRESS, out addr);
            int level;
            return TryGetBatteryLevel(addr == null ? null : addr.ToString(), ctx.DeviceName, out level)
                ? (int?)level : null;
        }

        /* ===================== Apple HID battery read ====================== */

        /// <summary>
        /// Obtain the battery level (0..100) of an Apple Magic device. Primary match is by
        /// Bluetooth MAC address (Apple HID devices report their MAC as the HID serial
        /// number). When the device looks like an Apple Magic device and exactly one Apple
        /// HID battery is present, it is used as a fallback so single-device setups work even
        /// if the serial/MAC can't be matched.
        /// </summary>
        private static bool TryGetBatteryLevel(string bluetoothAddress, string deviceName, out int level)
        {
            level = -1;
            try
            {
                    //macNormalized -> level, deduped across the device's HID collections
                Dictionary<string, int> readings = ReadAllAppleHidBatteries();
                if (readings.Count == 0)
                    return false;

                string target = NormalizeMac(bluetoothAddress);
                if (!String.IsNullOrEmpty(target) && readings.TryGetValue(target, out level))
                    return true;

                    //Fallback: a single Apple battery + an Apple-looking device name.
                    //With two Magic devices both produce readings, so this never fires and
                    //the MAC match above does the disambiguation.
                if (readings.Count == 1 && LooksLikeAppleDevice(deviceName))
                {
                    foreach (int only in readings.Values) { level = only; return true; }
                }
            }
            catch
            {
                    //Raw HID access can fail for many benign reasons (device asleep,
                    //access denied on a claimed collection). Treat as "no reading".
            }

            level = -1;
            return false;
        }

        private static Dictionary<string, int> ReadAllAppleHidBatteries()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (HidInterfaceInfo info in HidInterfaceEnumerator.Enumerate(appleVendorIds))
                ReadDeviceBattery(info, result);

            return result;
        }

        private static void ReadDeviceBattery(HidInterfaceInfo info, Dictionary<string, int> result)
        {
                //Report requests only -- this asks the device for report 0x90 rather than
                //waiting for it to send one, so the handle must not be overlapped.
            using (HidDevice device = HidDevice.OpenForReportRequests(info))
            {
                if (device == null)
                    return;

                    //Apple reports the MAC as the HID serial number for BT devices.
                string mac = NormalizeMac(device.GetSerialNumber());

                for (int attempt = 0; attempt < READ_ATTEMPTS; attempt++)
                {
                        //report id in byte[0]; on success byte[2] is the 0..100 level.
                    byte[] buffer = new byte[3];
                    buffer[0] = BATTERY_REPORT_ID;

                    if (device.GetInputReport(buffer) && buffer[0] == BATTERY_REPORT_ID)
                    {
                        int level = buffer[2];
                        if (level >= 0 && level <= 100)
                        {
                            result[mac] = level;   //keyed by MAC ("" if unknown) to dedupe collections
                            return;
                        }
                    }
                }
            }
        }

        private static string NormalizeMac(string value)
        {
            if (String.IsNullOrEmpty(value))
                return "";
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (Uri.IsHexDigit(c))
                    sb.Append(Char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private static bool LooksLikeAppleDevice(string name)
        {
            if (String.IsNullOrEmpty(name))
                return false;
            string n = name.ToLowerInvariant();
            return n.Contains("magic") || n.Contains("apple") || n.Contains("trackpad");
        }
    }
}
