using BluetoothLEBatteryMonitor.Service;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Windows.UI.Xaml.Automation.Peers;

namespace BluetoothLEBatteryMonitor
{
    public partial class Settings : Form
    {
        private DeviceManager deviceManager = null;
        private Info infoForm = null;
        private bool UserClose = false;
        private bool UserShow = false;
        private bool isInitializing = true;
        private bool updatingIcon = false;
        private Dictionary<string, NotifyIcon> deviceIcons = new Dictionary<string, NotifyIcon>();
        private Dictionary<string, bool> deviceLowBatteryNotificationDone = new Dictionary<string, bool>();

        public Settings()
        {
            InitializeComponent();

                //Force the form handle so worker-thread BeginInvoke can post to UI thread
            IntPtr _ = this.Handle;

                //First of all create entry for settings
            Registry.CurrentUser.CreateSubKey("SOFTWARE\\BluetoothLEBatteryMonitor");

                //Reload settings
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            if (rk != null)
                checkBoxStartup.Checked = rk.GetValue("BluetoothLEBatteryMonitor") != null;

            rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", false);

            numericUpDownRefreshPeriod.Value = (int)rk.GetValue("IntervalMin", 5);
            checkBoxNotification.Checked = ((int)rk.GetValue("NotificationEnabled", 1)) != 0;
            checkBoxScanForEver.Checked = ((int)rk.GetValue("AutomaticDetectionEnabled", 0)) != 0;
            checkBoxOneIconPerDevice.Checked = ((int)rk.GetValue("OneIconPerDevice", 0)) != 0;
            checkBoxHideUnknownBattery.Checked = ((int)rk.GetValue("HideUnknownBattery", 0)) != 0;

                //Instantiate everything
            deviceManager = new DeviceManager(new DeviceNotification(this));
            deviceManager.scan(checkBoxScanForEver.Checked);

            infoForm = new Info(deviceManager, () => checkBoxHideUnknownBattery.Checked);

            IconTimer.Interval = ((int)numericUpDownRefreshPeriod.Value) * 60 * 1000;
            IconTimer.Start();

            UpdateIcon();

                //Display the build version (CI patches AssemblyVersion via git describe)
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            label1.Text = String.Format("v{0} - by o0Zz (https://github.com/o0zz)", version);

            isInitializing = false;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(UserShow ? value : false);
            UserShow = false;
        }

        private void DeviceListForm_Load(object sender, EventArgs e)
        {

        }

        private void DeviceListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!UserClose)
                {
                    e.Cancel = true;
                    Hide();
                }
            }
        }

        private void IconTimer_Tick(object sender, EventArgs e)
        {
            UpdateIcon();
        }

        private static Icon GetIconForBatteryLevel(int level)
        {
            if (level < 0 || level >= 90) return BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_100;
            if (level >= 70) return BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_80;
            if (level >= 50) return BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_60;
            if (level >= 30) return BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_40;
            return BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_20;
        }

        public void UpdateIcon()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateIcon));
                return;
            }

                //refreshHidDevices below reports new devices synchronously, and OnNewDevice
                //calls back into here. The nested call has nothing left to do -- the device is
                //already in the dictionary this pass is about to walk -- so drop it.
            if (updatingIcon)
                return;

            updatingIcon = true;
            try
            {
                    //USB HID devices have no watcher behind them; re-snapshot them each tick
                    //so plugging or unplugging a dongle is picked up.
                deviceManager.refreshHidDevices();

                ConcurrentDictionary<string, BatteryDevice> deviceDict = deviceManager.getDeviceList();

                    //Request to update battery level
                foreach (BatteryDevice device in deviceDict.Values)
                    device.UpdateBatteryLevel();

                if (checkBoxOneIconPerDevice.Checked && !deviceDict.IsEmpty)
                    UpdateIconPerDevice(deviceDict);
                else
                    UpdateSingleIcon(deviceDict);
            }
            finally
            {
                updatingIcon = false;
            }
        }

        private void UpdateSingleIcon(ConcurrentDictionary<string, BatteryDevice> deviceDict)
        {
            ClearPerDeviceIcons();
            NotifyIcon.Visible = true;

            int theLowestBattery = 100;
            string theBalloonText = "";

            foreach (var kv in deviceDict)
            {
                int level = kv.Value.GetBatteryLevel();
                string name = kv.Value.GetName();

                if (level < 0 && checkBoxHideUnknownBattery.Checked)
                    continue;

                if ((level >= 0) && (level < theLowestBattery))
                    theLowestBattery = level;

                if (theBalloonText.Length != 0)
                    theBalloonText += "\n";

                theBalloonText += (level < 0)
                    ? String.Format("{0}: ?", name)
                    : String.Format("{0}: {1}%", name, level);

                NotifyLowBattery(kv.Key, name, level);
            }

            if (theLowestBattery > 0)
                NotifyIcon.Icon = GetIconForBatteryLevel(theLowestBattery);

            NotifyIcon.Text = theBalloonText.Substring(0, Math.Min(theBalloonText.Length, 64));
        }

        private void UpdateIconPerDevice(ConcurrentDictionary<string, BatteryDevice> deviceDict)
        {
            NotifyIcon.Visible = false;

            foreach (var kv in deviceDict)
            {
                int level = kv.Value.GetBatteryLevel();
                string name = kv.Value.GetName();

                if (level < 0 && checkBoxHideUnknownBattery.Checked)
                    continue;

                NotifyIcon icon;
                if (!deviceIcons.TryGetValue(kv.Key, out icon))
                {
                    icon = new NotifyIcon(this.components);
                    icon.ContextMenuStrip = this.contextMenuStrip;
                    icon.MouseDoubleClick += this.NotifyIcon_MouseDoubleClick;
                    icon.Visible = true;
                    deviceIcons[kv.Key] = icon;
                }

                icon.Icon = GetIconForBatteryLevel(level);
                string tooltip = (level < 0)
                    ? String.Format("{0}: ?", name)
                    : String.Format("{0}: {1}%", name, level);
                icon.Text = tooltip.Substring(0, Math.Min(tooltip.Length, 64));

                NotifyLowBattery(kv.Key, name, level);
            }

                //Drop icons for devices that disappeared from the manager
            foreach (string id in new List<string>(deviceIcons.Keys))
            {
                if (deviceDict.ContainsKey(id))
                    continue;

                deviceIcons[id].Visible = false;
                deviceIcons[id].Dispose();
                deviceIcons.Remove(id);
                deviceLowBatteryNotificationDone.Remove(id);
            }
        }

        private void NotifyLowBattery(string id, string name, int level)
        {
            if (level < 0)
                return;

            bool wasLow;
            deviceLowBatteryNotificationDone.TryGetValue(id, out wasLow);

            if (level <= 20 && !wasLow)
                Notify(String.Format("Battery LOW on '{0}' ({1}%) !", name, level), ToolTipIcon.Warning);

            deviceLowBatteryNotificationDone[id] = (level <= 20);
        }

        private void ClearPerDeviceIcons()
        {
            foreach (NotifyIcon icon in deviceIcons.Values)
            {
                icon.Visible = false;
                icon.Dispose();
            }
            deviceIcons.Clear();
        }


        public void Notify(string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (checkBoxNotification.Checked)
                NotifyIcon.ShowBalloonTip(300, "BluetoothLE Battery Monitor", message, icon);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            infoForm.Close();

            deviceManager.stopScan();

            ClearPerDeviceIcons();
            NotifyIcon.Visible = false;

                //Because of an issue, we have to show the setting form before closing it
            UserShow = true;
            Show();

            UserClose = true;
            Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserShow = true;
            Show();
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            infoForm.Show();
        }

        private void numericUpDownRefreshPeriod_ValueChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("IntervalMin", numericUpDownRefreshPeriod.Value, RegistryValueKind.DWord);

            IconTimer.Stop();
            IconTimer.Interval = (int)(numericUpDownRefreshPeriod.Value * 60 * 1000);
            IconTimer.Start();
        }
        private void checkBoxStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (checkBoxStartup.Checked)
                rk.SetValue("BluetoothLEBatteryMonitor", Application.ExecutablePath);
            else
                rk.DeleteValue("BluetoothLEBatteryMonitor", false);
        }

        private void checkBoxScanForEver_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("AutomaticDetectionEnabled", checkBoxScanForEver.Checked ? 1 : 0);

                //Restart the watchers so the new flag takes effect immediately
            deviceManager.stopScan();
            deviceManager.scan(checkBoxScanForEver.Checked);
        }

        private void checkBoxNotification_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("NotificationEnabled", checkBoxNotification.Checked ? 1 : 0);
        }

        private void checkBoxOneIconPerDevice_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("OneIconPerDevice", checkBoxOneIconPerDevice.Checked ? 1 : 0);
            UpdateIcon();
        }

        private void checkBoxHideUnknownBattery_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("HideUnknownBattery", checkBoxHideUnknownBattery.Checked ? 1 : 0);
            UpdateIcon();
        }
    }

    /* --------------------------------------------------------------------- */

    class DeviceNotification : IDeviceNotification
    {
        private Settings form;
        public DeviceNotification(Settings form)
        {
            this.form = form;
        }

        public void OnNewDevice(BatteryDevice aDevice)
        {
            //this.form.Notify("New device detected: " + aDevice.GetName() + " (Battery: " + aDevice.GetBatteryLevel() + "%)");
            this.form.UpdateIcon();
        }

        public void OnDeviceRemoved(string deviceId)
        {
            this.form.UpdateIcon();
        }

    }
}
