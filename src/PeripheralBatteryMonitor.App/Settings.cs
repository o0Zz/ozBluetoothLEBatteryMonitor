using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PeripheralBatteryMonitor
{
    public partial class Settings : Form
    {
        /// <summary>Where every user setting lives. Program reads Language from here too.</summary>
        internal const string RegistryPath = "SOFTWARE\\PeripheralBatteryMonitor";

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
            Registry.CurrentUser.CreateSubKey(RegistryPath);

                //Reload settings
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            if (rk != null)
                checkBoxStartup.Checked = rk.GetValue("PeripheralBatteryMonitor") != null;

            rk = Registry.CurrentUser.OpenSubKey(RegistryPath, false);

            numericUpDownRefreshPeriod.Value = (int)rk.GetValue("IntervalMin", 5);
            checkBoxNotification.Checked = ((int)rk.GetValue("NotificationEnabled", 1)) != 0;
            checkBoxScanForEver.Checked = ((int)rk.GetValue("AutomaticDetectionEnabled", 0)) != 0;
            checkBoxOneIconPerDevice.Checked = ((int)rk.GetValue("OneIconPerDevice", 0)) != 0;
            checkBoxHideUnknownBattery.Checked = ((int)rk.GetValue("HideUnknownBattery", 0)) != 0;

                //Program already applied this language before the window was built; the picker
                //only has to show which one it was.
            FillLanguages(Convert.ToString(rk.GetValue("Language", "")));
            ApplyStrings();

                //Instantiate everything
            deviceManager = new DeviceManager(new DeviceNotification(this));
            deviceManager.scan(checkBoxScanForEver.Checked);

            infoForm = new Info(deviceManager, () => checkBoxHideUnknownBattery.Checked);

            IconTimer.Interval = ((int)numericUpDownRefreshPeriod.Value) * 60 * 1000;
            IconTimer.Start();

            UpdateIcon();

            isInitializing = false;
        }

            //Auto-scaling has happened by the time OnLoad runs, which is what makes the
            //font measurements below the real ones. Same reason Info builds its ListView
            //columns here rather than in its constructor.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            FitInputRows();
        }

        /// <summary>
        /// Repairs the two rows that pair a caption with an input control, both of which
        /// WinForms gets wrong above 100% and neither of which can be fixed with a constant.
        ///
        /// The captions carry <c>Anchor.None</c> so a left-to-right FlowLayoutPanel centres
        /// them vertically on whatever the input control turns out to be. That only works if
        /// the input control's own geometry is honest, and for these two it is not:
        ///
        /// <list type="bullet">
        /// <item><description><b>NumericUpDown is a ContainerControl</b>, so it runs its own
        /// auto-scale pass on top of the form's and its margin is scaled more than once — a
        /// declared top margin of 3 arrived as 28 at 150%, which pushed the spinner nine pixels
        /// below its caption and stretched the row to 54 px. Copying the caption's margin puts
        /// it back on the one value that was scaled exactly once.</description></item>
        /// <item><description><b>ComboBox takes its height from its font once</b>, at
        /// construction, and never revisits it, so anything written to Size is pinned for the
        /// life of the form.</description></item>
        /// </list>
        /// </summary>
        private void FitInputRows()
        {
            numericUpDownRefreshPeriod.Margin = labelRefreshPeriod.Margin;
            FitLanguageBox();
        }

        /// <summary>
        /// Sizes the language box from its own font: height from what the control says it
        /// wants, width from the widest entry it has to show.
        ///
        /// Nothing here is a pixel constant, and that is the point. A hardcoded 160x21 was
        /// right at 100% and wrong at every other scale, and it would also have clipped a
        /// language whose "same as Windows" wording runs long.
        /// </summary>
        private void FitLanguageBox()
        {
            comboBoxLanguage.Height = comboBoxLanguage.PreferredHeight;

            int widest = 0;
            using (Graphics g = comboBoxLanguage.CreateGraphics())
            {
                foreach (object item in comboBoxLanguage.Items)
                {
                    int width = (int)Math.Ceiling(g.MeasureString(item.ToString(), comboBoxLanguage.Font).Width);
                    if (width > widest)
                        widest = width;
                }
            }

                //Room for the drop-down arrow, which is a system metric and so already scaled.
            comboBoxLanguage.Width = widest + SystemInformation.VerticalScrollBarWidth + comboBoxLanguage.Margin.Horizontal;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(UserShow ? value : false);
            UserShow = false;
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
            if (level < 0 || level >= 90) return PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_100;
            if (level >= 70) return PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_80;
            if (level >= 50) return PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_60;
            if (level >= 30) return PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_40;
            return PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_20;
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
            List<KeyValuePair<int, string>> known = new List<KeyValuePair<int, string>>();
            List<string> unknown = new List<string>();

            foreach (var kv in deviceDict)
            {
                int level = kv.Value.GetBatteryLevel();
                string name = kv.Value.GetName();

                if (level < 0 && checkBoxHideUnknownBattery.Checked)
                    continue;

                if (level < 0)
                {
                    unknown.Add(Strings.Format("tray.device.unknown", name));
                }
                else
                {
                    if (level < theLowestBattery)
                        theLowestBattery = level;

                    known.Add(new KeyValuePair<int, string>(level, Strings.Format("tray.device.known", name, level)));
                }

                NotifyLowBattery(kv.Key, name, level);
            }

                //The tooltip holds 63 characters and a device list can easily exceed that, so
                //the order the lines are written in decides which ones survive FitTooltip.
                //Devices with a real reading come first, lowest battery first -- that one is
                //what the tray icon is showing and the reason the user is hovering. Devices
                //still reading "?" carry no information, so they queue behind and are the first
                //thing dropped: before this, the dictionary's arbitrary order could spend the
                //whole tooltip on two "?" lines and cut away every real percentage.
            known.Sort((a, b) => a.Key.CompareTo(b.Key));

            List<string> lines = new List<string>(known.Count + unknown.Count);
            foreach (var entry in known)
                lines.Add(entry.Value);
            lines.AddRange(unknown);

            NotifyIcon.Icon = GetIconForBatteryLevel(theLowestBattery);

            NotifyIcon.Text = FitTooltip(lines);
        }

            //NotifyIcon.Text is a 64-character buffer *including* the terminator, so 63 is
            //the most WinForms accepts -- it throws ArgumentOutOfRangeException at 64, which
            //on the polling tick is an unhandled exception that kills the tray app. The old
            //Math.Min(length, 64) was off by exactly one and fired as soon as enough devices
            //were paired for their names to fill the tooltip.
        private const int TooltipLimit = 63;
        private const string TooltipMore = "…";

        /// <summary>
        /// Joins tooltip lines into what NotifyIcon.Text can actually hold, keeping the ones
        /// the caller put first.
        ///
        /// Lines are dropped whole rather than cut inside one -- the tooltip is one device per
        /// line and "Logi M650 L: 80%" cut mid-name reads as a different device. Dropping stops
        /// at the first line that does not fit instead of skipping it to squeeze in a shorter
        /// one further down, because the caller's order is a priority order.
        /// </summary>
        private static string FitTooltip(IList<string> lines)
        {
            if (lines.Count == 0)
                return "";

            int taken;
            string text = TakeLines(lines, TooltipLimit, out taken);
            if (taken == lines.Count)
                return text;

                //Room for the "…" line has to be reserved before the fit, not carved out of it.
            text = TakeLines(lines, TooltipLimit - TooltipMore.Length - 1, out taken);
            if (taken == 0)
                return lines[0].Substring(0, TooltipLimit - TooltipMore.Length) + TooltipMore;

            return text + "\n" + TooltipMore;
        }

        private static string TakeLines(IList<string> lines, int limit, out int taken)
        {
            StringBuilder text = new StringBuilder();
            taken = 0;

            foreach (string line in lines)
            {
                int cost = (text.Length == 0 ? 0 : 1) + line.Length;
                if (text.Length + cost > limit)
                    break;

                if (text.Length != 0)
                    text.Append('\n');

                text.Append(line);
                taken++;
            }

            return text.ToString();
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
                    ? Strings.Format("tray.device.unknown", name)
                    : Strings.Format("tray.device.known", name, level);
                icon.Text = FitTooltip(new string[] { tooltip });

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
                Notify(Strings.Format("notify.lowBattery", name, level), ToolTipIcon.Warning);

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
                NotifyIcon.ShowBalloonTip(300, Strings.Get("app.name"), message, icon);
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
            Activate();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowAbout();
        }

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            ShowAbout();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
                //Hide rather than Close: closing is what exiting means here, and
                //DeviceListForm_FormClosing would only cancel it and hide anyway.
            Hide();
        }

        private void ShowAbout()
        {
                //No owner: this form is usually hidden by SetVisibleCore, and ShowDialog
                //refuses an invisible owner outright.
            using (AboutForm about = new AboutForm())
                about.ShowDialog();
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            infoForm.Show();
        }

        private void numericUpDownRefreshPeriod_ValueChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
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
                rk.SetValue("PeripheralBatteryMonitor", Application.ExecutablePath);
            else
                rk.DeleteValue("PeripheralBatteryMonitor", false);
        }

        private void checkBoxScanForEver_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            rk.SetValue("AutomaticDetectionEnabled", checkBoxScanForEver.Checked ? 1 : 0);

                //Restart the watchers so the new flag takes effect immediately
            deviceManager.stopScan();
            deviceManager.scan(checkBoxScanForEver.Checked);
        }

        private void checkBoxNotification_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            rk.SetValue("NotificationEnabled", checkBoxNotification.Checked ? 1 : 0);
        }

        private void checkBoxOneIconPerDevice_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            rk.SetValue("OneIconPerDevice", checkBoxOneIconPerDevice.Checked ? 1 : 0);
            UpdateIcon();
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            Strings.Language chosen = comboBoxLanguage.SelectedItem as Strings.Language;
            string code = chosen == null ? "" : chosen.Code;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            rk.SetValue("Language", code, RegistryValueKind.String);

            Strings.Use(code);

                //Applied live rather than on next launch, which the panel-based layout makes
                //possible: every label is AutoSize inside an AutoSize form, so the window
                //re-measures itself around the new text instead of clipping it. The Info popup
                //is long-lived too and has to be told; the About box is built fresh each time
                //it opens, so it needs nothing.
            ApplyStrings();
            FitLanguageBox();
            infoForm.ApplyStrings();

                //The tray tooltip is built from translated text, so it stays stale until the
                //next poll unless it is rebuilt now.
            UpdateIcon();
        }

        /// <summary>Offers every language embedded in the exe, plus following Windows.</summary>
        private void FillLanguages(string savedCode)
        {
                //First entry, and the default: the right answer for most people is the language
                //their computer is already in.
            comboBoxLanguage.Items.Add(new Strings.Language("", Strings.Get("settings.language.auto")));

            foreach (Strings.Language language in Strings.Available)
                comboBoxLanguage.Items.Add(language);

            int chosen = 0;
            for (int i = 1; i < comboBoxLanguage.Items.Count; i++)
            {
                if (((Strings.Language)comboBoxLanguage.Items[i]).Code == savedCode)
                {
                    chosen = i;
                    break;
                }
            }
            comboBoxLanguage.SelectedIndex = chosen;
        }

        /// <summary>
        /// Pushes the current language onto every piece of text this window owns, including the
        /// tray menu.
        ///
        /// The designer keeps English literals so the WinForms designer still renders a sane
        /// form, and they double as the last-resort fallback; this overwrites them right after
        /// InitializeComponent and again whenever the language changes.
        /// </summary>
        private void ApplyStrings()
        {
            Text = Strings.Get("settings.title");

            settingsToolStripMenuItem.Text = Strings.Get("tray.settings");
            aboutToolStripMenuItem.Text = Strings.Get("tray.about");
            exitToolStripMenuItem.Text = Strings.Get("tray.exit");

            groupGeneral.Text = Strings.Get("settings.group.general");
            checkBoxStartup.Text = Strings.Get("settings.startup");
            hintStartup.Text = Strings.Get("settings.startup.hint");
            checkBoxNotification.Text = Strings.Get("settings.notifications");
            hintNotification.Text = Strings.Get("settings.notifications.hint");
            labelRefreshPeriod.Text = Strings.Get("settings.refresh");
            labelRefreshUnit.Text = Strings.Get("settings.refresh.unit");
            hintRefreshPeriod.Text = Strings.Get("settings.refresh.hint");
            labelLanguage.Text = Strings.Get("settings.language");
            hintLanguage.Text = Strings.Get("settings.language.hint");

            groupDevices.Text = Strings.Get("settings.group.devices");
            checkBoxScanForEver.Text = Strings.Get("settings.autoDetect");
            hintScanForEver.Text = Strings.Get("settings.autoDetect.hint");
            checkBoxOneIconPerDevice.Text = Strings.Get("settings.oneIconPerDevice");
            hintOneIconPerDevice.Text = Strings.Get("settings.oneIconPerDevice.hint");
            checkBoxHideUnknownBattery.Text = Strings.Get("settings.hideUnknown");
            hintHideUnknownBattery.Text = Strings.Get("settings.hideUnknown.hint");

            buttonAbout.Text = Strings.Get("button.about");
            buttonClose.Text = Strings.Get("button.close");

                //The "same as Windows" entry is the one combo item whose label is translated;
                //the rest name themselves and must not be.
            if (comboBoxLanguage.Items.Count > 0)
            {
                int selected = comboBoxLanguage.SelectedIndex;
                comboBoxLanguage.Items[0] = new Strings.Language("", Strings.Get("settings.language.auto"));
                comboBoxLanguage.SelectedIndex = selected;
            }
        }

        private void checkBoxHideUnknownBattery_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
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
