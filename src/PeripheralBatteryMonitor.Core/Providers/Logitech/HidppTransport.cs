using System;
using System.Diagnostics;
using PeripheralBatteryMonitor.Hid;

namespace PeripheralBatteryMonitor.Providers.Logitech
{
    /// <summary>
    /// Logitech HID++ 2.0 request/response framing over one HID interface.
    ///
    /// A request is an output report laid out as
    /// <c>[reportId][deviceIndex][featureIndex][functionId&lt;&lt;4 | softwareId][params...]</c>
    /// and the answer arrives as an input report echoing deviceIndex / featureIndex /
    /// functionId, so a reply can be told apart from the unsolicited notifications the device
    /// also sends on the same interface.
    ///
    /// Features are addressed by *index*, and the index of a given feature id differs per
    /// device, so it must be looked up at runtime through the root feature
    /// (<see cref="GetFeatureIndex"/>) rather than hardcoded.
    /// </summary>
    public class HidppTransport : IDisposable
    {
            //Long report: 20 bytes total (1 id + 19). Modern Logitech gaming gear exposes only
            //this one; the 7-byte short report (0x10) is a Unifying-era thing and writing it to
            //a collection that doesn't declare it fails outright.
        public const byte REPORT_LONG = 0x11;

            //Addresses the device sitting behind its own receiver, as opposed to indexes
            //1..6 which address devices paired to a multi-device Unifying receiver.
        public const byte DEVICE_INDEX_DIRECT = 0xFF;

        public const byte FEATURE_ROOT = 0x0000;

            //Any nonzero value; it is echoed back and distinguishes our traffic from
            //another application's (G HUB may be talking to the same device).
        private const byte SOFTWARE_ID = 0x0E;

            //A feature index of 0xFF in a reply marks a HID++ 2.0 error; 0x8F is the
            //HID++ 1.0 equivalent. Both mean "no value", never "index 255".
        private const byte ERROR_HIDPP20 = 0xFF;
        private const byte ERROR_HIDPP10 = 0x8F;

        private const int MAX_FRAMES_PER_REQUEST = 8;

            //Arbitrary byte echoed back by the root feature's ping, which is what makes a
            //pong tell-apart-able from any other reply.
        private const byte PING_MAGIC = 0xAA;

        private HidDevice device;

        private HidppTransport(HidDevice device)
        {
            this.device = device;
        }

        /// <summary>
        /// Open a HID++ conversation on the given interface. Null when the interface can't be
        /// opened or is too small to carry a long report.
        /// </summary>
        public static HidppTransport Open(HidInterfaceInfo info)
        {
            if (info == null || info.OutputReportByteLength < 20 || info.InputReportByteLength < 20)
                return null;

            HidDevice hid = HidDevice.Open(info);
            if (hid == null)
                return null;

            return new HidppTransport(hid);
        }

        /// <summary>
        /// Run one transaction. Returns the raw reply frame -- byte 4 onwards is the payload --
        /// or null on write failure, timeout or a device-reported error.
        /// </summary>
        public byte[] Request(byte deviceIndex, byte featureIndex, byte functionId, byte[] parameters, int timeoutMs)
        {
            byte[] request = new byte[device.OutputReportByteLength];
            request[0] = REPORT_LONG;
            request[1] = deviceIndex;
            request[2] = featureIndex;
            request[3] = (byte)((functionId << 4) | SOFTWARE_ID);
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length && (4 + i) < request.Length; i++)
                    request[4 + i] = parameters[i];
            }

            if (!device.Write(request, timeoutMs))
                return null;

                //Bound the whole exchange, not just each individual read: a chatty device
                //could otherwise keep us here for MAX_FRAMES_PER_REQUEST * timeoutMs.
            Stopwatch budget = Stopwatch.StartNew();

            for (int frame = 0; frame < MAX_FRAMES_PER_REQUEST; frame++)
            {
                int remaining = timeoutMs - (int)budget.ElapsedMilliseconds;
                if (remaining <= 0)
                    return null;

                byte[] reply = new byte[device.InputReportByteLength];
                int read;
                if (!device.Read(reply, remaining, out read))
                    return null;

                if (read < 5)
                    continue;
                if (reply[1] != deviceIndex)
                    continue;   //notification about some other device on this receiver

                if (reply[2] == ERROR_HIDPP20 || reply[2] == ERROR_HIDPP10)
                {
                        //Error frame: [id][devIdx][0xFF][echoed featureIndex][echoed func][code]
                    if (read > 5 && reply[3] == featureIndex)
                    {
                        Debug.WriteLine("[HID++] error on feature 0x" + featureIndex.ToString("X2")
                            + " func 0x" + functionId.ToString("X2") + ": code 0x" + reply[5].ToString("X2"));
                        return null;
                    }
                    continue;
                }

                if (reply[2] == featureIndex && reply[3] == request[3])
                    return reply;

                    //Anything else is an unsolicited notification; keep waiting for our reply.
            }

            return null;
        }

        /// <summary>
        /// Root feature ping. Cheap way to find out whether anything is actually listening
        /// before spending a timeout per feature probing what it supports -- a device that is
        /// switched off answers nothing at all.
        /// </summary>
        public bool Ping(byte deviceIndex, int timeoutMs)
        {
            byte[] reply = Request(deviceIndex, FEATURE_ROOT, 0x01, new byte[] { 0x00, 0x00, PING_MAGIC }, timeoutMs);
            return reply != null && reply.Length > 6 && reply[6] == PING_MAGIC;
        }

        /// <summary>
        /// Resolve a feature id to this device's feature index via the root feature.
        /// Returns 0 when the device does not implement it (index 0 is always the root
        /// feature itself, so it is never a valid answer here).
        /// </summary>
        public byte GetFeatureIndex(byte deviceIndex, ushort featureId, int timeoutMs)
        {
            byte[] reply = Request(deviceIndex, FEATURE_ROOT, 0x00,
                new byte[] { (byte)(featureId >> 8), (byte)(featureId & 0xFF), 0x00 }, timeoutMs);

            if (reply == null || reply.Length < 5)
                return 0;
            return reply[4];
        }

        public void Dispose()
        {
            if (device != null)
            {
                device.Dispose();
                device = null;
            }
        }
    }
}
