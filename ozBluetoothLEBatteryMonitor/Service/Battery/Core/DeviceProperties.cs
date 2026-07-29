namespace BluetoothLEBatteryMonitor.Service.Battery.Core
{
    /// <summary>
    /// Canonical keys into a device's property bag. Centralised here (Core) so both
    /// the discovery layer (which requests them) and the providers (which read them) depend
    /// on this, not on each other.
    /// </summary>
    public static class DeviceProperties
    {
        /* ---- Keys delivered by WinRT (DeviceInformation.Properties) ---- */

            //DEVPROPKEY string-form: "{guid} pid". DEVPKEY_Device_BatteryLevel reports a byte 0..100.
        public const string PROP_BATTERY_LEVEL = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
            //Coarse battery enum (Critical/Low/Average/Full).
        public const string PROP_BATTERY_LIFE = "System.Devices.BatteryLife";
        public const string PROP_AEP_IS_CONNECTED = "System.Devices.Aep.IsConnected";
        public const string PROP_AEP_IS_PAIRED = "System.Devices.Aep.IsPaired";
        public const string PROP_AEP_DEVICE_ADDRESS = "System.Devices.Aep.DeviceAddress";

        /* ---- Keys this app synthesises for HID-discovered devices ---- */

            //Set by the HID discovery source, not by WinRT. The "oz." prefix keeps them from
            //ever colliding with a canonical Windows property name. They let a provider reopen
            //the exact interface a device was found on without re-enumerating the HID stack.
        public const string PROP_HID_PATH = "oz.Hid.DevicePath";
        public const string PROP_HID_VENDOR_ID = "oz.Hid.VendorId";
        public const string PROP_HID_PRODUCT_ID = "oz.Hid.ProductId";
        public const string PROP_HID_USAGE_PAGE = "oz.Hid.UsagePage";
        public const string PROP_HID_USAGE = "oz.Hid.Usage";
        public const string PROP_HID_INPUT_REPORT_LENGTH = "oz.Hid.InputReportByteLength";
        public const string PROP_HID_OUTPUT_REPORT_LENGTH = "oz.Hid.OutputReportByteLength";
    }
}
