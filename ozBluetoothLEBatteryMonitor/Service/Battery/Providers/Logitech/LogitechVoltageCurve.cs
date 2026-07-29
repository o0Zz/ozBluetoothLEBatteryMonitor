namespace BluetoothLEBatteryMonitor.Service.Battery.Providers.Logitech
{
    /// <summary>
    /// Converts a single-cell Li-Po terminal voltage into a rough charge percentage.
    ///
    /// Needed because devices that only implement the ADC_MEASUREMENT feature report
    /// millivolts, not a percentage -- the mapping lives in the vendor's software, not in the
    /// device. The discharge curve below is the one Solaar uses for Logitech peripherals and
    /// it is an <b>approximation</b>: the curve is flat between roughly 3.7 V and 4.0 V, so a
    /// few millivolts of noise there move the result by several percent, and it does not
    /// account for load or temperature. Expect the number to be in the right band rather
    /// than exact, and to differ by a few percent from what Logitech G HUB shows.
    ///
    /// Tune by editing the table: it must stay ordered from highest voltage to lowest.
    /// </summary>
    public static class LogitechVoltageCurve
    {
        private static readonly int[,] curve = new int[,]
        {
            //  mV,  percent
            { 4186, 100 },
            { 4067,  90 },
            { 3989,  80 },
            { 3922,  70 },
            { 3859,  60 },
            { 3811,  50 },
            { 3778,  40 },
            { 3751,  30 },
            { 3717,  20 },
            { 3671,  10 },
            { 3646,   5 },
            { 3579,   2 },
            { 3500,   0 },
        };

        /// <summary>
        /// Percentage (0..100) for a terminal voltage in millivolts. Values above/below the
        /// curve clamp to 100 / 0; in between the two neighbouring points are interpolated so
        /// the reading moves smoothly instead of snapping between table rows.
        /// </summary>
        public static int ToPercentage(int millivolts)
        {
            int last = curve.GetLength(0) - 1;

            if (millivolts >= curve[0, 0])
                return curve[0, 1];
            if (millivolts <= curve[last, 0])
                return curve[last, 1];

            for (int i = 0; i < last; i++)
            {
                int highMv = curve[i, 0], highPct = curve[i, 1];
                int lowMv = curve[i + 1, 0], lowPct = curve[i + 1, 1];

                if (millivolts > lowMv)
                {
                        //Linear interpolation between the bracketing points.
                    int span = highMv - lowMv;
                    if (span <= 0)
                        return lowPct;
                    return lowPct + ((millivolts - lowMv) * (highPct - lowPct)) / span;
                }
            }

            return curve[last, 1];
        }
    }
}
