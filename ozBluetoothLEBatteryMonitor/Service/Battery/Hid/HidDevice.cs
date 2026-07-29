using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BluetoothLEBatteryMonitor.Service.Battery.Hid
{
    /// <summary>
    /// An open HID interface that can be written to and read from with a timeout.
    ///
    /// The handle is opened with FILE_FLAG_OVERLAPPED because HID reads block until the
    /// device sends an input report -- and a silent device (headset switched off, dongle
    /// asleep) would otherwise hang the caller forever. Since the poll loop runs on the UI
    /// thread, every operation is bounded: the I/O is issued asynchronously, waited on for
    /// at most <c>timeoutMs</c>, then cancelled.
    ///
    /// Open one only for the duration of a transaction. The driver keeps a per-handle queue
    /// of input reports, so a short-lived handle guarantees the first report read is a
    /// response to what was just written rather than something stale.
    /// </summary>
    public class HidDevice : IDisposable
    {
        private SafeFileHandle handle;

        public int InputReportByteLength { get; private set; }
        public int OutputReportByteLength { get; private set; }

        private HidDevice(SafeFileHandle handle, int inputLength, int outputLength)
        {
            this.handle = handle;
            this.InputReportByteLength = inputLength;
            this.OutputReportByteLength = outputLength;
        }

        /// <summary>Open an interface for read/write. Null when it can't be opened.</summary>
        public static HidDevice Open(HidInterfaceInfo info)
        {
            if (info == null || String.IsNullOrEmpty(info.Path))
                return null;

            SafeFileHandle h = HidNative.CreateFile(info.Path,
                HidNative.GENERIC_READ | HidNative.GENERIC_WRITE,
                HidNative.FILE_SHARE_READ | HidNative.FILE_SHARE_WRITE,
                IntPtr.Zero, HidNative.OPEN_EXISTING, HidNative.FILE_FLAG_OVERLAPPED, IntPtr.Zero);

            if (h.IsInvalid)
            {
                h.Dispose();
                return null;
            }

            return new HidDevice(h, info.InputReportByteLength, info.OutputReportByteLength);
        }

        /// <summary>
        /// Send an output report. <paramref name="report"/> must be
        /// <see cref="OutputReportByteLength"/> bytes long with the report id in byte 0 --
        /// the HID class driver rejects anything shorter and trims the padding itself.
        /// </summary>
        public bool Write(byte[] report, int timeoutMs)
        {
            if (report == null || report.Length != OutputReportByteLength)
                return false;

            IntPtr evt = HidNative.CreateEvent(IntPtr.Zero, true, false, null);
            IntPtr overlapped = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(HidNative.OVERLAPPED)));
            try
            {
                HidNative.OVERLAPPED o = new HidNative.OVERLAPPED();
                o.hEvent = evt;
                Marshal.StructureToPtr(o, overlapped, false);

                uint written;
                if (HidNative.WriteFile(handle, report, (uint)report.Length, out written, overlapped))
                    return true;

                if (Marshal.GetLastWin32Error() != HidNative.ERROR_IO_PENDING)
                    return false;

                if (HidNative.WaitForSingleObject(evt, (uint)timeoutMs) != HidNative.WAIT_OBJECT_0)
                {
                    HidNative.CancelIo(handle);
                    return false;
                }

                return HidNative.GetOverlappedResult(handle, overlapped, out written, false);
            }
            finally
            {
                Marshal.FreeHGlobal(overlapped);
                HidNative.CloseHandle(evt);
            }
        }

        /// <summary>
        /// Wait for one input report. Returns false on timeout, which for a HID device is a
        /// normal outcome (nothing to say) rather than an error.
        /// </summary>
        public bool Read(byte[] buffer, int timeoutMs, out int bytesRead)
        {
            bytesRead = 0;
            if (buffer == null || buffer.Length < InputReportByteLength)
                return false;

            IntPtr evt = HidNative.CreateEvent(IntPtr.Zero, true, false, null);
            IntPtr overlapped = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(HidNative.OVERLAPPED)));
            try
            {
                HidNative.OVERLAPPED o = new HidNative.OVERLAPPED();
                o.hEvent = evt;
                Marshal.StructureToPtr(o, overlapped, false);

                uint read;
                if (HidNative.ReadFile(handle, buffer, (uint)InputReportByteLength, out read, overlapped))
                {
                    bytesRead = (int)read;
                    return true;
                }

                if (Marshal.GetLastWin32Error() != HidNative.ERROR_IO_PENDING)
                    return false;

                if (HidNative.WaitForSingleObject(evt, (uint)timeoutMs) != HidNative.WAIT_OBJECT_0)
                {
                    HidNative.CancelIo(handle);
                    return false;
                }

                if (!HidNative.GetOverlappedResult(handle, overlapped, out read, false))
                    return false;

                bytesRead = (int)read;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(overlapped);
                HidNative.CloseHandle(evt);
            }
        }

        /// <summary>
        /// Ask the device for an input report by id (GET_REPORT on the control pipe) instead
        /// of waiting for it to be sent. <paramref name="report"/> carries the requested id
        /// in byte 0. Unlike <see cref="Read"/> this never blocks on device traffic.
        /// </summary>
        public bool GetInputReport(byte[] report)
        {
            if (report == null || report.Length == 0)
                return false;
            return HidNative.HidD_GetInputReport(handle, report, report.Length);
        }

        /// <summary>The HID serial number string, or "" when the device doesn't expose one.</summary>
        public string GetSerialNumber()
        {
            byte[] buffer = new byte[254];   //HID string cap is 126 wchars incl. null
            if (!HidNative.HidD_GetSerialNumberString(handle, buffer, buffer.Length))
                return "";
            string s = Encoding.Unicode.GetString(buffer);
            int nul = s.IndexOf('\0');
            if (nul >= 0)
                s = s.Substring(0, nul);
            return s;
        }

        public void Dispose()
        {
            if (handle != null)
            {
                handle.Dispose();
                handle = null;
            }
        }
    }
}
