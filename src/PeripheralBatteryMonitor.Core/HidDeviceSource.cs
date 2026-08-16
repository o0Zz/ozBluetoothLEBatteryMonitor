using System;
using System.Collections.Generic;
using PeripheralBatteryMonitor.Battery.Core;
using PeripheralBatteryMonitor.Battery.Hid;
using PeripheralBatteryMonitor.Battery.Providers;

namespace PeripheralBatteryMonitor
{
    /// <summary>
    /// Discovery source for devices that reach the PC over raw USB HID instead of Bluetooth --
    /// typically a wireless peripheral with its own vendor dongle, which has no Bluetooth
    /// association endpoint and so is invisible to the <c>DeviceWatcher</c> pair in
    /// <see cref="DeviceManager"/>.
    ///
    /// Unlike Bluetooth discovery there is nothing to subscribe to: this is a plain snapshot
    /// of what is plugged in right now, cheap enough (a setupapi walk filtered to a handful of
    /// vendor ids) to re-run on every poll tick, which is also what gives plug/unplug
    /// handling for free.
    /// </summary>
    public static class HidDeviceSource
    {
        /// <summary>Every present HID interface that a registered spec claims. May be empty.</summary>
        public static List<HidInterfaceInfo> Discover()
        {
            List<HidInterfaceInfo> matched = new List<HidInterfaceInfo>();

            ICollection<ushort> vendorIds = HidDeviceSpecRegistry.GetVendorIds();
            if (vendorIds.Count == 0)
                return matched;

            foreach (HidInterfaceInfo info in HidInterfaceEnumerator.Enumerate(vendorIds))
            {
                if (HidDeviceSpecRegistry.Match(info) != null)
                    matched.Add(info);
            }

            return matched;
        }

        /// <summary>
        /// Key for the device dictionary. The interface path plays the same role as
        /// <c>DeviceInformation.Id</c> does for Bluetooth: opaque, and unique per device per
        /// USB port. Moving the dongle to another port therefore reads as a different device,
        /// which is the same behaviour as re-pairing a Bluetooth device.
        /// </summary>
        public static string GetDeviceId(HidInterfaceInfo info)
        {
            return info.Path;
        }

        public static string GetDeviceName(HidInterfaceInfo info)
        {
            HidDeviceSpec spec = HidDeviceSpecRegistry.Match(info);
            if (spec != null)
                return spec.NameFor(info);
            return String.IsNullOrWhiteSpace(info.Product) ? "Unknown HID device" : info.Product.Trim();
        }

        /// <summary>
        /// Seed the device's property bag with everything a provider needs to reopen this
        /// exact interface, so reading a battery never has to re-enumerate the HID stack.
        /// </summary>
        public static Dictionary<string, object> GetProperties(HidInterfaceInfo info)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties[DeviceProperties.PROP_HID_PATH] = info.Path;
            properties[DeviceProperties.PROP_HID_VENDOR_ID] = (int)info.VendorId;
            properties[DeviceProperties.PROP_HID_PRODUCT_ID] = (int)info.ProductId;
            properties[DeviceProperties.PROP_HID_USAGE_PAGE] = (int)info.UsagePage;
            properties[DeviceProperties.PROP_HID_USAGE] = (int)info.Usage;
            properties[DeviceProperties.PROP_HID_INPUT_REPORT_LENGTH] = info.InputReportByteLength;
            properties[DeviceProperties.PROP_HID_OUTPUT_REPORT_LENGTH] = info.OutputReportByteLength;
            properties[DeviceProperties.PROP_HID_FEATURE_REPORT_LENGTH] = info.FeatureReportByteLength;
            return properties;
        }
    }
}
