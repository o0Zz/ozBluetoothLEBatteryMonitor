using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Radios;

namespace PeripheralBatteryMonitor
{
    /// <summary>
    /// Turns the machine's Bluetooth radios off and back on -- the same thing as the
    /// Bluetooth toggle in Windows Settings, which is what tears the stack down and brings
    /// it back up. It exists because a wedged stack is the one failure this app cannot poll
    /// its way out of: every provider keeps timing out until something resets the radio.
    ///
    /// <para>Why <see cref="Radio"/> and not the <c>bthserv</c> service: stopping the
    /// Bluetooth Support Service needs administrator rights and does not touch the radio,
    /// so it would fix less while asking for more. Disabling the device node in the device
    /// tree does reset it, but also needs elevation. The radio toggle needs neither.</para>
    ///
    /// <para>A restart takes at least the requested downtime, so the caller must not run it
    /// on the UI thread -- except for <see cref="RequestAccess"/>, which has the opposite
    /// requirement. See its own remarks.</para>
    /// </summary>
    public static class BluetoothRadio
    {
            //Both are WinRT calls that talk to the radio driver. Generous, because the
            //failure mode this whole class exists for is a stack that has stopped answering:
            //hanging for ten seconds is a better report than declaring failure over a slow
            //driver that would have come back.
        private const int radioCallTimeoutMs = 10000;
        private const int accessRequestTimeoutMs = 10000;

        /// <summary>
        /// Asks Windows for permission to change radio state, and throws if it says no.
        ///
        /// <para><b>Call this on the UI thread</b>, before handing the rest of the work to a
        /// worker. This is the one call in the class that can show a consent prompt, and a
        /// WinRT call that may put UI on screen wants a thread with a message loop; the
        /// desktop answer is normally an immediate Allowed, but that is not something to
        /// depend on from a thread pool thread.</para>
        /// </summary>
        public static void RequestAccess()
        {
            Task<RadioAccessStatus> access = Radio.RequestAccessAsync().AsTask();
            if (!access.Wait(accessRequestTimeoutMs))
                throw new TimeoutException("Windows did not answer the request for radio access.");

            if (access.Result != RadioAccessStatus.Allowed)
                throw new InvalidOperationException("Windows denied access to the radios (" + access.Result + ").");
        }

        /// <summary>
        /// True if this machine has a Bluetooth radio at all, so the caller can leave the
        /// menu entry out rather than offering something that can only fail.
        /// </summary>
        public static bool IsAvailable()
        {
            try
            {
                return GetBluetoothRadios().Count > 0;
            }
            catch (Exception)
            {
                    //No radio, no Bluetooth driver, or WinRT refusing to enumerate -- all of
                    //which mean the same thing to the caller.
                return false;
            }
        }

        /// <summary>
        /// Switches every Bluetooth radio off, waits <paramref name="downtimeMs"/>, and
        /// switches them back on. Blocks for at least that long, and throws if any step
        /// fails -- including on the way back up, which is the failure worth reporting: the
        /// radio is then off and only this method can be asked to try again.
        /// </summary>
        public static void Restart(int downtimeMs)
        {
            IList<Radio> radios = GetBluetoothRadios();
            if (radios.Count == 0)
                throw new InvalidOperationException("No Bluetooth radio found on this machine.");

            SetState(radios, RadioState.Off);

            Thread.Sleep(downtimeMs);

                //Re-enumerate rather than reusing the handles from above: a radio that went
                //off can come back as a different Radio instance, and the stale one then
                //refuses the state change. Name is the only identity a Radio exposes -- there
                //is no Id on this type -- so that is what the old and new lists are matched
                //on, falling back to the instance we already hold when nothing matches.
            SetState(Rebind(radios), RadioState.On);
        }

        private static IList<Radio> GetBluetoothRadios()
        {
            Task<IReadOnlyList<Radio>> radiosTask = Radio.GetRadiosAsync().AsTask();
            if (!radiosTask.Wait(radioCallTimeoutMs))
                throw new TimeoutException("Windows did not answer the request for the radio list.");

            List<Radio> bluetooth = new List<Radio>();
            foreach (Radio radio in radiosTask.Result)
            {
                if (radio.Kind == RadioKind.Bluetooth)
                    bluetooth.Add(radio);
            }
            return bluetooth;
        }

        private static IList<Radio> Rebind(IList<Radio> radios)
        {
            IList<Radio> current;
            try
            {
                current = GetBluetoothRadios();
            }
            catch (Exception)
            {
                return radios;
            }

            List<Radio> rebound = new List<Radio>();
            foreach (Radio was in radios)
            {
                Radio match = was;
                foreach (Radio now in current)
                {
                    if (String.Equals(now.Name, was.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        match = now;
                        break;
                    }
                }
                rebound.Add(match);
            }
            return rebound;
        }

        private static void SetState(IList<Radio> radios, RadioState state)
        {
            foreach (Radio radio in radios)
            {
                Task<RadioAccessStatus> stateTask = radio.SetStateAsync(state).AsTask();
                if (!stateTask.Wait(radioCallTimeoutMs))
                    throw new TimeoutException("The Bluetooth radio did not answer a request to switch " + (state == RadioState.On ? "on" : "off") + ".");

                if (stateTask.Result != RadioAccessStatus.Allowed)
                    throw new InvalidOperationException("Could not switch the Bluetooth radio " + (state == RadioState.On ? "on" : "off") + " (" + stateTask.Result + ").");
            }
        }
    }
}
