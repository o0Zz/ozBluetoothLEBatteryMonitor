using System;
using System.Diagnostics;
using PeripheralBatteryMonitor.Service.Battery.Core;
using PeripheralBatteryMonitor.Service.Battery.Hid;

namespace PeripheralBatteryMonitor.Service.Battery.Providers.Logitech
{
    /// <summary>
    /// Reads battery for Logitech LIGHTSPEED devices that talk to their own USB dongle -- the
    /// PRO X Wireless headset to begin with. These are not Bluetooth devices at all, so they
    /// are discovered through the HID layer (see <see cref="HidSpec"/>) and none of the
    /// Bluetooth property-bag providers can see them; Windows exposes no battery for them
    /// either (<see cref="DeviceProperties.PROP_BATTERY_LEVEL"/> is absent from every one of
    /// the device's nodes, and there is no HID-battery node).
    ///
    /// Battery comes from HID++ 2.0 over the device's vendor collection. Which *feature*
    /// carries it varies per device, so <see cref="batteryFeatures"/> is probed in order and
    /// the first one that both exists and yields a value is remembered. Features that report a
    /// percentage outright come first; the two that report a raw cell voltage are last,
    /// because turning volts into a percentage costs accuracy (see
    /// <see cref="LogitechVoltageCurve"/>).
    ///
    /// Verified against a PRO X Wireless (VID 0x046D / PID 0x0ABA), which is the awkward case:
    /// it implements *none* of the three standard battery features, only 0x1F20. HID++ 4.2
    /// answers on its 0xFF43/0x0202 collection at device index 0xFF, and 0x1F20 function 0
    /// returns [voltage_hi][voltage_lo][flags], e.g. 0x0F54 = 3924 mV.
    /// </summary>
    public class LogitechBatteryProvider : IBatteryProvider
    {
        private const ushort LOGITECH_VENDOR_ID = 0x046D;

            //Logitech's HID++ collection on modern gaming gear.
        private const ushort HIDPP_USAGE_PAGE = 0xFF43;
        private const ushort HIDPP_USAGE = 0x0202;

        /* ---- HID++ battery features, most to least precise ---- */

            //0x1004 UNIFIED_BATTERY: func 1 getStatus -> [stateOfCharge][levels][charging][ext].
            //State of charge is a straight percentage when the device supports it.
        private const ushort FEATURE_UNIFIED_BATTERY = 0x1004;
            //0x1000 BATTERY_UNIFIED_LEVEL_STATUS: func 0 -> [level][nextLevel][status], level
            //being a percentage, or 0 when the device only reports discrete levels.
        private const ushort FEATURE_BATTERY_LEVEL_STATUS = 0x1000;
            //0x1001 BATTERY_VOLTAGE: func 0 -> [mV_hi][mV_lo][flags].
        private const ushort FEATURE_BATTERY_VOLTAGE = 0x1001;
            //0x1F20 ADC_MEASUREMENT: func 0 -> [mV_hi][mV_lo][flags]. What the PRO X uses.
        private const ushort FEATURE_ADC_MEASUREMENT = 0x1F20;

        private static readonly ushort[] batteryFeatures = new ushort[]
        {
            FEATURE_UNIFIED_BATTERY,
            FEATURE_BATTERY_LEVEL_STATUS,
            FEATURE_BATTERY_VOLTAGE,
            FEATURE_ADC_MEASUREMENT,
        };

            //The dongle answers in well under a millisecond when the device is awake; this
            //only has to cover the case where it is off and says nothing at all.
        private const int TIMEOUT_MS = 500;

        /// <summary>
        /// The HID interface that stands for one of these devices, for the discovery layer.
        ///
        /// Deliberately restricted to product ids that have actually been verified: matching
        /// every Logitech HID++ collection would also match Unifying receivers for mice and
        /// keyboards, where device index 0xFF addresses the receiver rather than the
        /// peripheral and no battery feature answers -- surfacing a phantom "unknown battery"
        /// entry in the tray. Adding a LIGHTSPEED device is a one-line change here: its
        /// battery feature no longer has to be the same one, but it does still have to sit on
        /// this collection and answer at device index 0xFF.
        /// </summary>
        public static readonly HidDeviceSpec HidSpec = new HidDeviceSpec(
            LOGITECH_VENDOR_ID,
            new ushort[] { 0x0ABA },      //PRO X Wireless Gaming Headset
            HIDPP_USAGE_PAGE,
            HIDPP_USAGE,
            "Logitech Wireless Headset");

            //The battery feature this device turned out to use, resolved once and kept: feature
            //indexes are per-device but stable for its lifetime, so steady-state polling costs
            //a single transaction. One provider instance exists per device, so nothing leaks
            //between them. Index 0 always means the root feature, hence "not resolved yet".
        private ushort boundFeatureId = 0;
        private byte boundFeatureIndex = 0;

        public int? ReadBattery(IBatteryDeviceContext ctx)
        {
                //Cheap rejections first: this runs against every tracked device, including
                //every Bluetooth one, on every poll.
            if (ctx == null || ctx.Transport != DeviceTransport.UsbHid)
                return null;

            int vendorId;
            if (!TryGetInt(ctx, DeviceProperties.PROP_HID_VENDOR_ID, out vendorId) || vendorId != LOGITECH_VENDOR_ID)
                return null;

            HidInterfaceInfo info = DescribeFromProperties(ctx);
            if (info == null)
                return null;

            try
            {
                using (HidppTransport hidpp = HidppTransport.Open(info))
                {
                    if (hidpp == null)
                        return null;

                    if (boundFeatureIndex != 0)
                        return Read(hidpp, boundFeatureId, boundFeatureIndex, ctx);

                    return Resolve(hidpp, ctx);
                }
            }
            catch (Exception e)
            {
                    //Raw HID access fails for plenty of benign reasons (dongle yanked
                    //mid-transaction, another process holding the collection). No reading.
                Debug.WriteLine("[Logitech] read failed on '" + ctx.DeviceName + "': " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Find which battery feature this device carries, and bind to the first that actually
        /// produces a value. Only called until one sticks.
        /// </summary>
        private int? Resolve(HidppTransport hidpp, IBatteryDeviceContext ctx)
        {
                //Probing costs one timeout per feature, so make sure something is listening
                //first -- otherwise a device that is simply switched off would burn the whole
                //chain's worth of timeouts on the UI thread, every poll.
            if (!hidpp.Ping(HidppTransport.DEVICE_INDEX_DIRECT, TIMEOUT_MS))
            {
                Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' is not answering HID++ (switched off?)");
                return null;
            }

            foreach (ushort featureId in batteryFeatures)
            {
                byte featureIndex = hidpp.GetFeatureIndex(HidppTransport.DEVICE_INDEX_DIRECT, featureId, TIMEOUT_MS);
                if (featureIndex == 0)
                    continue;   //not implemented by this device

                int? level = Read(hidpp, featureId, featureIndex, ctx);
                if (!level.HasValue)
                    continue;   //implemented but not answering / no value to give

                boundFeatureId = featureId;
                boundFeatureIndex = featureIndex;
                Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' bound to feature 0x"
                    + featureId.ToString("X4") + " at index 0x" + featureIndex.ToString("X2"));
                return level;
            }

            Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' implements no known battery feature");
            return null;
        }

        /// <summary>Read and decode one battery feature. Null when it has nothing to report.</summary>
        private int? Read(HidppTransport hidpp, ushort featureId, byte featureIndex, IBatteryDeviceContext ctx)
        {
            switch (featureId)
            {
                case FEATURE_UNIFIED_BATTERY:
                    return ReadUnifiedBattery(hidpp, featureIndex, ctx);

                case FEATURE_BATTERY_LEVEL_STATUS:
                    return ReadLevelStatus(hidpp, featureIndex, ctx);

                case FEATURE_BATTERY_VOLTAGE:
                case FEATURE_ADC_MEASUREMENT:
                    return ReadVoltage(hidpp, featureId, featureIndex, ctx);
            }
            return null;
        }

        /// <summary>0x1004 getStatus: a percentage when supported, else a discrete level flag.</summary>
        private int? ReadUnifiedBattery(HidppTransport hidpp, byte featureIndex, IBatteryDeviceContext ctx)
        {
            byte[] reply = hidpp.Request(HidppTransport.DEVICE_INDEX_DIRECT, featureIndex, 0x01, null, TIMEOUT_MS);
            if (reply == null || reply.Length < 7)
                return null;

            int stateOfCharge = reply[4];
            if (stateOfCharge >= 1 && stateOfCharge <= 100)
            {
                Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' unifiedBattery soc=" + stateOfCharge + "%");
                return stateOfCharge;
            }

                //State of charge not supported: fall back to the discrete level bitfield. This
                //really is a coarse four-way enum, so each level becomes a representative
                //percentage -- a band, not a measurement.
            int level = reply[5];
            if ((level & 0x08) != 0) return 90;   //full
            if ((level & 0x04) != 0) return 60;   //good
            if ((level & 0x02) != 0) return 30;   //low
            if ((level & 0x01) != 0) return 10;   //critical
            return null;
        }

        /// <summary>0x1000 getBatteryLevelStatus: percentage in byte 0, 0 meaning "unknown".</summary>
        private int? ReadLevelStatus(HidppTransport hidpp, byte featureIndex, IBatteryDeviceContext ctx)
        {
            byte[] reply = hidpp.Request(HidppTransport.DEVICE_INDEX_DIRECT, featureIndex, 0x00, null, TIMEOUT_MS);
            if (reply == null || reply.Length < 7)
                return null;

            int dischargeLevel = reply[4];
            if (dischargeLevel < 1 || dischargeLevel > 100)
                return null;

            Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' levelStatus=" + dischargeLevel
                + "% (status=0x" + reply[6].ToString("X2") + ")");
            return dischargeLevel;
        }

        /// <summary>0x1001 / 0x1F20: raw cell voltage in millivolts, converted by the curve.</summary>
        private int? ReadVoltage(HidppTransport hidpp, ushort featureId, byte featureIndex, IBatteryDeviceContext ctx)
        {
            byte[] reply = hidpp.Request(HidppTransport.DEVICE_INDEX_DIRECT, featureIndex, 0x00, null, TIMEOUT_MS);
            if (reply == null || reply.Length < 7)
                return null;    //device switched off / out of range

            int millivolts = (reply[4] << 8) | reply[5];
                //Sanity-bound it: a single Li-Po cell that reads outside this window means the
                //payload isn't what we think it is, and guessing a percentage from it would be
                //worse than reporting nothing.
            if (millivolts < 2000 || millivolts > 5000)
            {
                Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' implausible voltage " + millivolts
                    + "mV from feature 0x" + featureId.ToString("X4") + " -- ignored");
                return null;
            }

            int percent = LogitechVoltageCurve.ToPercentage(millivolts);
            Debug.WriteLine("[Logitech] '" + ctx.DeviceName + "' " + millivolts + "mV -> " + percent
                + "% (feature 0x" + featureId.ToString("X4") + ", flags=0x" + reply[6].ToString("X2") + ")");
            return percent;
        }

        /// <summary>
        /// Rebuild the HID interface descriptor from what the discovery layer cached in the
        /// property bag, so reading a device costs no re-enumeration.
        /// </summary>
        private static HidInterfaceInfo DescribeFromProperties(IBatteryDeviceContext ctx)
        {
            object path;
            if (!ctx.TryGetProperty(DeviceProperties.PROP_HID_PATH, out path) || path == null)
                return null;

            int inputLength, outputLength;
            if (!TryGetInt(ctx, DeviceProperties.PROP_HID_INPUT_REPORT_LENGTH, out inputLength))
                return null;
            if (!TryGetInt(ctx, DeviceProperties.PROP_HID_OUTPUT_REPORT_LENGTH, out outputLength))
                return null;

            HidInterfaceInfo info = new HidInterfaceInfo();
            info.Path = path.ToString();
            info.InputReportByteLength = inputLength;
            info.OutputReportByteLength = outputLength;
            return info;
        }

        private static bool TryGetInt(IBatteryDeviceContext ctx, string key, out int value)
        {
            value = 0;
            object raw;
            if (!ctx.TryGetProperty(key, out raw) || raw == null)
                return false;
            try
            {
                value = Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
