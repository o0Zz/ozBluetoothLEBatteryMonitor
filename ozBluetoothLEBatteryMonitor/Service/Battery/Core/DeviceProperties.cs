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

            //DEVPROPKEY string-form: "{guid} pid". A byte 0..100 -- a **percentage**.
            //
            //Windows surfaces this same property under two names, and the property bag carries
            //both spellings side by side: this raw DEVPROPKEY, and the canonical name
            //"System.Devices.BatteryLife" (documented with exactly this formatID and propID 2).
            //They are aliases of one value, not two sources -- so read it once, through this key.
            //There is no coarse Critical/Low/Average/Full enum behind either spelling; a
            //provider that switched on 1..4 here would silently turn a nearly-flat battery
            //(3%) into a healthy-looking one (60%).
            //
            //Both spellings are delivered as null while the device is disconnected.
        public const string PROP_BATTERY_LEVEL = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
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
