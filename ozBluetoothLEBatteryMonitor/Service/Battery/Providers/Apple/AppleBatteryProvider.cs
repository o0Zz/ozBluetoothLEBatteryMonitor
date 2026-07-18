using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using BluetoothLEBatteryMonitor.Service.Battery.Core;

namespace BluetoothLEBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// Reads battery for Apple "Magic" devices (Magic Mouse / Magic Trackpad / Magic
    /// Keyboard). These do NOT expose battery through the standard Bluetooth properties
    /// (System.Devices.BatteryLife / DEVPKEY_Device_BatteryLevel), so the other providers
    /// never see them. Instead they report battery through a vendor HID input report
    /// (report id 0x90), where byte[2] holds the level as a plain 0..100 percentage -- only
    /// reachable through the raw HID stack (hid.dll over a CreateFile handle), which this
    /// provider drives directly. Verified against the Linux hid-magicmouse driver and the
    /// WinMagicBattery Windows implementation.
    ///
    /// <see cref="ReadBattery"/> is definitive: it yields a value only when a matching Apple
    /// HID battery is actually found for this device.
    /// </summary>
    public class AppleBatteryProvider : IBatteryProvider
    {
        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
            object addr;
            ctx.TryGetProperty(DeviceProperties.PROP_AEP_DEVICE_ADDRESS, out addr);
            int level;
            return TryGetBatteryLevel(addr == null ? null : addr.ToString(), ctx.DeviceName, out level)
                ? (int?)level : null;
        }

        /* ===================== Apple HID battery read ====================== */

            //Apple vendor id, as reported by HidD_GetAttributes. 0x004C over Bluetooth,
            //0x05AC over USB. (The device path spells the BT one "vid&0002004c".)
        private const ushort APPLE_VID_BT = 0x004C;
        private const ushort APPLE_VID_USB = 0x05AC;

        private const byte BATTERY_REPORT_ID = 0x90;   //Apple vendor battery input report

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

            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);

            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet == INVALID_HANDLE_VALUE)
                return result;

            try
            {
                SP_DEVICE_INTERFACE_DATA interfaceData = new SP_DEVICE_INTERFACE_DATA();
                interfaceData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

                for (uint index = 0;
                     SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData);
                     index++)
                {
                    string path = GetDevicePath(deviceInfoSet, ref interfaceData);
                    if (path == null)
                        continue;

                        //Cheap pre-filter to avoid opening every unrelated HID device.
                        //BT path contains "vid&0002004c", USB path contains "vid_05ac".
                    string lower = path.ToLowerInvariant();
                    if (!lower.Contains("004c") && !lower.Contains("05ac"))
                        continue;

                    ReadDeviceBattery(path, result);
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        private static void ReadDeviceBattery(string path, Dictionary<string, int> result)
        {
            using (SafeFileHandle handle = CreateFile(path,
                       GENERIC_READ | GENERIC_WRITE,
                       FILE_SHARE_READ | FILE_SHARE_WRITE,
                       IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid)
                    return;

                HIDD_ATTRIBUTES attr = new HIDD_ATTRIBUTES();
                attr.Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES));
                if (!HidD_GetAttributes(handle, ref attr))
                    return;
                if (attr.VendorID != APPLE_VID_BT && attr.VendorID != APPLE_VID_USB)
                    return;

                    //Apple reports the MAC as the HID serial number for BT devices.
                string mac = NormalizeMac(GetSerialNumber(handle));

                    //report id in byte[0]; on success byte[2] is the 0..100 level.
                    //Retry a few times: the first GET_REPORT after wake can return stale/fail.
                for (int i = 0; i < 3; i++)
                {
                    byte[] buffer = new byte[3];
                    buffer[0] = BATTERY_REPORT_ID;
                    if (HidD_GetInputReport(handle, buffer, buffer.Length)
                        && buffer[0] == BATTERY_REPORT_ID)
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

        private static string GetDevicePath(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA interfaceData)
        {
            int requiredSize = 0;
                //First call sizes the buffer.
            SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, ref requiredSize, IntPtr.Zero);
            if (requiredSize <= 0)
                return null;

            IntPtr detailBuffer = Marshal.AllocHGlobal(requiredSize);
            try
            {
                    //cbSize of SP_DEVICE_INTERFACE_DETAIL_DATA: 8 on 64-bit, 6 on 32-bit (4 + sizeof(WCHAR)).
                Marshal.WriteInt32(detailBuffer, (IntPtr.Size == 8) ? 8 : 6);

                if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, detailBuffer, requiredSize, ref requiredSize, IntPtr.Zero))
                    return null;

                    //The DevicePath string starts right after the cbSize DWORD.
                return Marshal.PtrToStringAuto(new IntPtr(detailBuffer.ToInt64() + 4));
            }
            finally
            {
                Marshal.FreeHGlobal(detailBuffer);
            }
        }

        private static string GetSerialNumber(SafeFileHandle handle)
        {
            byte[] buffer = new byte[254];   //HID string cap is 126 wchars incl. null
            if (!HidD_GetSerialNumberString(handle, buffer, buffer.Length))
                return null;
            string s = Encoding.Unicode.GetString(buffer);
            int nul = s.IndexOf('\0');
            if (nul >= 0)
                s = s.Substring(0, nul);
            return s;
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

        /* ------------------------- P/Invoke ------------------------------ */

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetAttributes(SafeFileHandle handle, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetSerialNumberString(SafeFileHandle handle, byte[] buffer, int bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetInputReport(SafeFileHandle handle, byte[] reportBuffer, int reportBufferLength);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
    }
}
