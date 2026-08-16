using System;
using PeripheralBatteryMonitor.Battery.Core;
using PeripheralBatteryMonitor.Battery.Hid;

namespace PeripheralBatteryMonitor.Battery.Providers
{
    /// <summary>
    /// Shared plumbing for providers that reopen the exact HID interface a device was
    /// discovered on.
    ///
    /// This lives in <c>Providers/</c> rather than in <c>Hid/</c> or <c>Core/</c> because it is
    /// the only layer that legitimately depends on both: it turns a
    /// <see cref="IBatteryDeviceContext"/> (Core) into a <see cref="HidInterfaceInfo"/> (Hid).
    /// Putting it in either of those would make one depend on the other and collapse the
    /// separation the two folders exist for.
    /// </summary>
    internal static class ProviderHid
    {
        /// <summary>
        /// Rebuild the interface description <c>HidDeviceSource</c> put in the property bag, so
        /// a provider can open that one collection without walking the HID stack again. Null
        /// when the device did not come from HID discovery, or the bag is incomplete.
        /// </summary>
        internal static HidInterfaceInfo DescribeFromProperties(IBatteryDeviceContext ctx)
        {
            if (ctx == null)
                return null;

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

                //Optional: a collection with no feature reports simply leaves this 0, which is
                //the same thing the enumerator would have recorded.
            int featureLength;
            if (TryGetInt(ctx, DeviceProperties.PROP_HID_FEATURE_REPORT_LENGTH, out featureLength))
                info.FeatureReportByteLength = featureLength;

            return info;
        }

        /// <summary>
        /// Read a property that should hold a number. The bag is <c>object</c>-typed and its
        /// HID entries are boxed ints, but going through <see cref="Convert"/> keeps a
        /// differently-boxed value from silently reading as absent.
        /// </summary>
        internal static bool TryGetInt(IBatteryDeviceContext ctx, string key, out int value)
        {
            value = 0;
            object raw;
            if (ctx == null || !ctx.TryGetProperty(key, out raw) || raw == null)
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
