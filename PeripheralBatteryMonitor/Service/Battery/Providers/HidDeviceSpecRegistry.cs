using System;
using System.Collections.Generic;
using PeripheralBatteryMonitor.Service.Battery.Hid;
using PeripheralBatteryMonitor.Service.Battery.Providers.Logitech;

namespace PeripheralBatteryMonitor.Service.Battery.Providers
{
    /// <summary>
    /// The HID interfaces the discovery layer should surface as tracked devices. Companion to
    /// <see cref="BatteryProviderRegistry"/>: that one says *how* to read a battery, this one
    /// says which non-Bluetooth devices exist in the first place.
    ///
    /// Only devices with no Bluetooth association endpoint belong here -- a Bluetooth device
    /// is already found by the watchers in <c>DeviceManager</c>, and adding it here too would
    /// list it twice. That is why the Apple Magic devices, which are also read over raw HID,
    /// register nothing.
    /// </summary>
    public static class HidDeviceSpecRegistry
    {
        private static readonly List<HidDeviceSpec> specs = new List<HidDeviceSpec>();

        static HidDeviceSpecRegistry()
        {
            Register(LogitechBatteryProvider.HidSpec);   //Logitech LIGHTSPEED (PRO X Wireless headset)
        }

        /// <summary>Add a spec. Do this before the first scan for it to take effect.</summary>
        public static void Register(HidDeviceSpec spec)
        {
            if (spec == null) throw new ArgumentNullException("spec");
            specs.Add(spec);
        }

        public static IList<HidDeviceSpec> GetSpecs()
        {
            return specs;
        }

        /// <summary>
        /// The distinct vendor ids across every spec, so enumeration can skip opening HID
        /// interfaces that could never match.
        /// </summary>
        public static ICollection<ushort> GetVendorIds()
        {
            List<ushort> vendorIds = new List<ushort>();
            foreach (HidDeviceSpec spec in specs)
            {
                if (!vendorIds.Contains(spec.VendorId))
                    vendorIds.Add(spec.VendorId);
            }
            return vendorIds;
        }

        /// <summary>The first spec matching this interface, or null.</summary>
        public static HidDeviceSpec Match(HidInterfaceInfo info)
        {
            foreach (HidDeviceSpec spec in specs)
            {
                if (spec.Matches(info))
                    return spec;
            }
            return null;
        }
    }
}
