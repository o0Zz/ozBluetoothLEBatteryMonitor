using System;

namespace PeripheralBatteryMonitor.Battery.Hid
{
    /// <summary>
    /// Describes the one HID interface that stands for a physical device the app should
    /// track. Needed because a device publishes several collections and because most HID
    /// devices are not battery powered at all -- discovery only surfaces interfaces a
    /// provider has declared it can read (see <c>HidDeviceSpecRegistry</c>).
    ///
    /// Only register a spec for devices that have <b>no</b> Bluetooth association endpoint.
    /// A Bluetooth device (an Apple Magic Mouse, say) is already discovered by the Bluetooth
    /// watchers, and registering it here would surface it a second time.
    /// </summary>
    public class HidDeviceSpec
    {
        public readonly ushort VendorId;
        public readonly ushort[] ProductIds;   //null or empty = any product of this vendor
        public readonly ushort UsagePage;      //0 = any
        public readonly ushort Usage;          //0 = any
        public readonly string FallbackName;   //used when the device exposes no product string

        public HidDeviceSpec(ushort vendorId, ushort[] productIds, ushort usagePage, ushort usage, string fallbackName)
        {
            this.VendorId = vendorId;
            this.ProductIds = productIds;
            this.UsagePage = usagePage;
            this.Usage = usage;
            this.FallbackName = fallbackName;
        }

        public bool Matches(HidInterfaceInfo info)
        {
            if (info == null)
                return false;
            if (info.VendorId != VendorId)
                return false;
            if (UsagePage != 0 && info.UsagePage != UsagePage)
                return false;
            if (Usage != 0 && info.Usage != Usage)
                return false;

            if (ProductIds != null && ProductIds.Length > 0)
            {
                bool found = false;
                foreach (ushort pid in ProductIds)
                {
                    if (info.ProductId == pid) { found = true; break; }
                }
                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>Display name for a matched interface: the device's own string, else the fallback.</summary>
        public string NameFor(HidInterfaceInfo info)
        {
            if (info != null && !String.IsNullOrWhiteSpace(info.Product))
                return info.Product.Trim();
            return FallbackName;
        }
    }
}
