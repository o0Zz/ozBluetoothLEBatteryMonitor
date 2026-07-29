namespace BluetoothLEBatteryMonitor.Service.Battery.Hid
{
    /// <summary>
    /// One HID interface (a single top-level collection) as reported by the OS. A physical
    /// device usually publishes several of these -- e.g. the Logitech PRO X Wireless dongle
    /// exposes a consumer-control collection plus two vendor-defined ones -- so the usage
    /// page / usage pair is what identifies the collection worth talking to.
    /// </summary>
    public class HidInterfaceInfo
    {
        public string Path;             //\\?\hid#vid_046d&pid_0aba&mi_03&col02#... (open with HidDevice)
        public ushort VendorId;
        public ushort ProductId;
        public ushort UsagePage;        //0xFF00-0xFFFF for vendor-defined collections
        public ushort Usage;
        public int InputReportByteLength;
        public int OutputReportByteLength;
        public string Product;          //HID product string; may be empty

        public override string ToString()
        {
            return string.Format("VID_{0:X4}&PID_{1:X4} UP=0x{2:X4} U=0x{3:X4} '{4}'",
                VendorId, ProductId, UsagePage, Usage, Product);
        }
    }
}
