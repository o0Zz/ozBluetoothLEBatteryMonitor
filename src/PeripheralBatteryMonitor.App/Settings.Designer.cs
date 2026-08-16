namespace PeripheralBatteryMonitor
{
    partial class Settings
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.NotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.IconTimer = new System.Windows.Forms.Timer(this.components);
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.groupGeneral = new System.Windows.Forms.GroupBox();
            this.layoutGeneral = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxStartup = new System.Windows.Forms.CheckBox();
            this.hintStartup = new System.Windows.Forms.Label();
            this.checkBoxNotification = new System.Windows.Forms.CheckBox();
            this.hintNotification = new System.Windows.Forms.Label();
            this.layoutRefreshPeriod = new System.Windows.Forms.FlowLayoutPanel();
            this.labelRefreshPeriod = new System.Windows.Forms.Label();
            this.numericUpDownRefreshPeriod = new System.Windows.Forms.NumericUpDown();
            this.labelRefreshUnit = new System.Windows.Forms.Label();
            this.hintRefreshPeriod = new System.Windows.Forms.Label();
            this.groupDevices = new System.Windows.Forms.GroupBox();
            this.layoutDevices = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxScanForEver = new System.Windows.Forms.CheckBox();
            this.hintScanForEver = new System.Windows.Forms.Label();
            this.checkBoxOneIconPerDevice = new System.Windows.Forms.CheckBox();
            this.hintOneIconPerDevice = new System.Windows.Forms.Label();
            this.checkBoxHideUnknownBattery = new System.Windows.Forms.CheckBox();
            this.hintHideUnknownBattery = new System.Windows.Forms.Label();
            this.layoutButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonAbout = new System.Windows.Forms.Button();
            this.contextMenuStrip.SuspendLayout();
            this.layoutRoot.SuspendLayout();
            this.groupGeneral.SuspendLayout();
            this.layoutGeneral.SuspendLayout();
            this.layoutRefreshPeriod.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRefreshPeriod)).BeginInit();
            this.groupDevices.SuspendLayout();
            this.layoutDevices.SuspendLayout();
            this.layoutButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // NotifyIcon
            //
            this.NotifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.NotifyIcon.Icon = global::PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_100;
            this.NotifyIcon.Text = "Peripheral Battery Monitor";
            this.NotifyIcon.Visible = true;
            this.NotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIcon_MouseDoubleClick);
            //
            // contextMenuStrip
            //
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem,
            this.aboutToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip1";
            //
            // settingsToolStripMenuItem
            //
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            //
            // aboutToolStripMenuItem
            //
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            //
            // toolStripMenuItem1
            //
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            //
            // exitToolStripMenuItem
            //
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            //
            // IconTimer
            //
            this.IconTimer.Interval = 120000;
            this.IconTimer.Tick += new System.EventHandler(this.IconTimer_Tick);
            //
            // layoutRoot
            //
            // One column at 100% so both group boxes stretch to the same width, and
            // AutoSize rows so each one is exactly as tall as its own contents. The
            // trailing 100% row is an empty spacer that keeps the groups at the top
            // if the window is ever taller than they are.
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.groupGeneral, 0, 0);
            this.layoutRoot.Controls.Add(this.groupDevices, 0, 1);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.AutoSize = true;
            this.layoutRoot.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutRoot.Location = new System.Drawing.Point(0, 0);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.Padding = new System.Windows.Forms.Padding(12, 12, 12, 0);
            this.layoutRoot.RowCount = 3;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.TabIndex = 0;
            //
            // groupGeneral
            //
            this.groupGeneral.Controls.Add(this.layoutGeneral);
            this.groupGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupGeneral.AutoSize = true;
            this.groupGeneral.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupGeneral.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.groupGeneral.Name = "groupGeneral";
            this.groupGeneral.Padding = new System.Windows.Forms.Padding(10, 4, 10, 8);
            this.groupGeneral.TabIndex = 0;
            this.groupGeneral.TabStop = false;
            this.groupGeneral.Text = "General";
            //
            // layoutGeneral
            //
            this.layoutGeneral.AutoSize = true;
            this.layoutGeneral.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutGeneral.Controls.Add(this.checkBoxStartup);
            this.layoutGeneral.Controls.Add(this.hintStartup);
            this.layoutGeneral.Controls.Add(this.checkBoxNotification);
            this.layoutGeneral.Controls.Add(this.hintNotification);
            this.layoutGeneral.Controls.Add(this.layoutRefreshPeriod);
            this.layoutGeneral.Controls.Add(this.hintRefreshPeriod);
            this.layoutGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutGeneral.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.layoutGeneral.Margin = new System.Windows.Forms.Padding(0);
            this.layoutGeneral.Name = "layoutGeneral";
            this.layoutGeneral.TabIndex = 0;
            this.layoutGeneral.WrapContents = false;
            //
            // checkBoxStartup
            //
            this.checkBoxStartup.AutoSize = true;
            this.checkBoxStartup.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.checkBoxStartup.Name = "checkBoxStartup";
            this.checkBoxStartup.TabIndex = 0;
            this.checkBoxStartup.Text = "Launch application on startup";
            this.checkBoxStartup.UseVisualStyleBackColor = true;
            this.checkBoxStartup.CheckedChanged += new System.EventHandler(this.checkBoxStartup_CheckedChanged);
            //
            // hintStartup
            //
            this.hintStartup.AutoSize = true;
            this.hintStartup.ForeColor = System.Drawing.SystemColors.GrayText;
            this.hintStartup.Margin = new System.Windows.Forms.Padding(20, 0, 3, 10);
            this.hintStartup.MaximumSize = new System.Drawing.Size(400, 0);
            this.hintStartup.Name = "hintStartup";
            this.hintStartup.Text = "Start with Windows, straight into the tray. Off by default.";
            //
            // checkBoxNotification
            //
            this.checkBoxNotification.AutoSize = true;
            this.checkBoxNotification.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBoxNotification.Name = "checkBoxNotification";
            this.checkBoxNotification.TabIndex = 1;
            this.checkBoxNotification.Text = "Enable notifications";
            this.checkBoxNotification.UseVisualStyleBackColor = true;
            this.checkBoxNotification.CheckedChanged += new System.EventHandler(this.checkBoxNotification_CheckedChanged);
            //
            // hintNotification
            //
            this.hintNotification.AutoSize = true;
            this.hintNotification.ForeColor = System.Drawing.SystemColors.GrayText;
            this.hintNotification.Margin = new System.Windows.Forms.Padding(20, 0, 3, 10);
            this.hintNotification.MaximumSize = new System.Drawing.Size(400, 0);
            this.hintNotification.Name = "hintNotification";
            this.hintNotification.Text = "Show a balloon once each time a device drops to 20% or below, not on every refresh while it stays there.";
            //
            // layoutRefreshPeriod
            //
            this.layoutRefreshPeriod.AutoSize = true;
            this.layoutRefreshPeriod.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutRefreshPeriod.Controls.Add(this.labelRefreshPeriod);
            this.layoutRefreshPeriod.Controls.Add(this.numericUpDownRefreshPeriod);
            this.layoutRefreshPeriod.Controls.Add(this.labelRefreshUnit);
            this.layoutRefreshPeriod.Margin = new System.Windows.Forms.Padding(0);
            this.layoutRefreshPeriod.Name = "layoutRefreshPeriod";
            this.layoutRefreshPeriod.TabIndex = 2;
            this.layoutRefreshPeriod.WrapContents = false;
            //
            // labelRefreshPeriod
            //
            this.labelRefreshPeriod.AutoSize = true;
            this.labelRefreshPeriod.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.labelRefreshPeriod.Name = "labelRefreshPeriod";
            this.labelRefreshPeriod.Text = "Refresh period:";
            //
            // numericUpDownRefreshPeriod
            //
            this.numericUpDownRefreshPeriod.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.numericUpDownRefreshPeriod.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.numericUpDownRefreshPeriod.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownRefreshPeriod.Name = "numericUpDownRefreshPeriod";
            this.numericUpDownRefreshPeriod.Size = new System.Drawing.Size(65, 20);
            this.numericUpDownRefreshPeriod.TabIndex = 0;
            this.numericUpDownRefreshPeriod.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDownRefreshPeriod.ValueChanged += new System.EventHandler(this.numericUpDownRefreshPeriod_ValueChanged);
            //
            // labelRefreshUnit
            //
            this.labelRefreshUnit.AutoSize = true;
            this.labelRefreshUnit.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.labelRefreshUnit.Name = "labelRefreshUnit";
            this.labelRefreshUnit.Text = "min";
            //
            // hintRefreshPeriod
            //
            this.hintRefreshPeriod.AutoSize = true;
            this.hintRefreshPeriod.ForeColor = System.Drawing.SystemColors.GrayText;
            this.hintRefreshPeriod.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.hintRefreshPeriod.MaximumSize = new System.Drawing.Size(400, 0);
            this.hintRefreshPeriod.Name = "hintRefreshPeriod";
            this.hintRefreshPeriod.Text = "How often every tracked device is polled. Between 1 minute and 24 hours.";
            //
            // groupDevices
            //
            this.groupDevices.Controls.Add(this.layoutDevices);
            this.groupDevices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupDevices.AutoSize = true;
            this.groupDevices.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupDevices.Margin = new System.Windows.Forms.Padding(0);
            this.groupDevices.Name = "groupDevices";
            this.groupDevices.Padding = new System.Windows.Forms.Padding(10, 4, 10, 8);
            this.groupDevices.TabIndex = 1;
            this.groupDevices.TabStop = false;
            this.groupDevices.Text = "Devices and tray icons";
            //
            // layoutDevices
            //
            this.layoutDevices.AutoSize = true;
            this.layoutDevices.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutDevices.Controls.Add(this.checkBoxScanForEver);
            this.layoutDevices.Controls.Add(this.hintScanForEver);
            this.layoutDevices.Controls.Add(this.checkBoxOneIconPerDevice);
            this.layoutDevices.Controls.Add(this.hintOneIconPerDevice);
            this.layoutDevices.Controls.Add(this.checkBoxHideUnknownBattery);
            this.layoutDevices.Controls.Add(this.hintHideUnknownBattery);
            this.layoutDevices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutDevices.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.layoutDevices.Margin = new System.Windows.Forms.Padding(0);
            this.layoutDevices.Name = "layoutDevices";
            this.layoutDevices.TabIndex = 0;
            this.layoutDevices.WrapContents = false;
            //
            // checkBoxScanForEver
            //
            this.checkBoxScanForEver.AutoSize = true;
            this.checkBoxScanForEver.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.checkBoxScanForEver.Name = "checkBoxScanForEver";
            this.checkBoxScanForEver.TabIndex = 0;
            this.checkBoxScanForEver.Text = "Automatically detect new devices";
            this.checkBoxScanForEver.UseVisualStyleBackColor = true;
            this.checkBoxScanForEver.CheckedChanged += new System.EventHandler(this.checkBoxScanForEver_CheckedChanged);
            //
            // hintScanForEver
            //
            this.hintScanForEver.AutoSize = true;
            this.hintScanForEver.ForeColor = System.Drawing.SystemColors.GrayText;
            this.hintScanForEver.Margin = new System.Windows.Forms.Padding(20, 0, 3, 10);
            this.hintScanForEver.MaximumSize = new System.Drawing.Size(400, 0);
            this.hintScanForEver.Name = "hintScanForEver";
            this.hintScanForEver.Text = "Keep watching for newly paired Bluetooth devices. When off, only devices already paired at startup are tracked. USB dongles are always picked up.";
            //
            // checkBoxOneIconPerDevice
            //
            this.checkBoxOneIconPerDevice.AutoSize = true;
            this.checkBoxOneIconPerDevice.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBoxOneIconPerDevice.Name = "checkBoxOneIconPerDevice";
            this.checkBoxOneIconPerDevice.TabIndex = 1;
            this.checkBoxOneIconPerDevice.Text = "Show one tray icon per device";
            this.checkBoxOneIconPerDevice.UseVisualStyleBackColor = true;
            this.checkBoxOneIconPerDevice.CheckedChanged += new System.EventHandler(this.checkBoxOneIconPerDevice_CheckedChanged);
            //
            // hintOneIconPerDevice
            //
            this.hintOneIconPerDevice.AutoSize = true;
            this.hintOneIconPerDevice.ForeColor = System.Drawing.SystemColors.GrayText;
            this.hintOneIconPerDevice.Margin = new System.Windows.Forms.Padding(20, 0, 3, 10);
            this.hintOneIconPerDevice.MaximumSize = new System.Drawing.Size(400, 0);
            this.hintOneIconPerDevice.Name = "hintOneIconPerDevice";
            this.hintOneIconPerDevice.Text = "When off, one icon shows the lowest battery of all devices and its tooltip lists each one.";
            //
            // checkBoxHideUnknownBattery
            //
            this.checkBoxHideUnknownBattery.AutoSize = true;
            this.checkBoxHideUnknownBattery.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBoxHideUnknownBattery.Name = "checkBoxHideUnknownBattery";
            this.checkBoxHideUnknownBattery.TabIndex = 2;
            this.checkBoxHideUnknownBattery.Text = "Hide devices with unknown battery level";
            this.checkBoxHideUnknownBattery.UseVisualStyleBackColor = true;
            this.checkBoxHideUnknownBattery.CheckedChanged += new System.EventHandler(this.checkBoxHideUnknownBattery_CheckedChanged);
            //
            // hintHideUnknownBattery
            //
            this.hintHideUnknownBattery.AutoSize = true;
            this.hintHideUnknownBattery.ForeColor = System.Drawing.SystemColors.GrayText;
            this.hintHideUnknownBattery.Margin = new System.Windows.Forms.Padding(20, 0, 3, 0);
            this.hintHideUnknownBattery.MaximumSize = new System.Drawing.Size(400, 0);
            this.hintHideUnknownBattery.Name = "hintHideUnknownBattery";
            this.hintHideUnknownBattery.Text = "Some devices report no battery to Windows at all. When on, they are left out of the tray, the tooltip and the device list.";
            //
            // layoutButtons
            //
            // RightToLeft flow, so the control added first sits rightmost.
            this.layoutButtons.AutoSize = true;
            this.layoutButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutButtons.Controls.Add(this.buttonClose);
            this.layoutButtons.Controls.Add(this.buttonAbout);
            this.layoutButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.layoutButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.layoutButtons.Name = "layoutButtons";
            this.layoutButtons.Padding = new System.Windows.Forms.Padding(8, 8, 12, 10);
            this.layoutButtons.TabIndex = 1;
            //
            // buttonClose
            //
            this.buttonClose.AutoSize = true;
            this.buttonClose.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonClose.MinimumSize = new System.Drawing.Size(84, 26);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.TabIndex = 0;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            //
            // buttonAbout
            //
            this.buttonAbout.AutoSize = true;
            this.buttonAbout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonAbout.MinimumSize = new System.Drawing.Size(84, 26);
            this.buttonAbout.Name = "buttonAbout";
            this.buttonAbout.TabIndex = 1;
            this.buttonAbout.Text = "About...";
            this.buttonAbout.UseVisualStyleBackColor = true;
            this.buttonAbout.Click += new System.EventHandler(this.buttonAbout_Click);
            //
            // Settings
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // AutoSize rather than a fixed ClientSize: every row in here is a wrapped
            // label, so the height that fits depends on the display scale. Letting the
            // form ask its own contents is the only version that is right at 100% and
            // at 150%.
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            // Docking is applied from the highest index down, so the filled panel has to
            // sit at index 0 -- added first -- to be laid out last and take what is left.
            this.Controls.Add(this.layoutRoot);
            this.Controls.Add(this.layoutButtons);
            // Enter and Escape both dismiss the window. There is no Cancel: every
            // setting is written to the registry the moment it changes, so there is
            // nothing pending for a Cancel to discard.
            this.AcceptButton = this.buttonClose;
            this.CancelButton = this.buttonClose;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = global::PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_100;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Peripheral Battery Monitor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DeviceListForm_FormClosing);
            this.contextMenuStrip.ResumeLayout(false);
            this.layoutRoot.ResumeLayout(false);
            this.layoutRoot.PerformLayout();
            this.groupGeneral.ResumeLayout(false);
            this.groupGeneral.PerformLayout();
            this.layoutGeneral.ResumeLayout(false);
            this.layoutGeneral.PerformLayout();
            this.layoutRefreshPeriod.ResumeLayout(false);
            this.layoutRefreshPeriod.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRefreshPeriod)).EndInit();
            this.groupDevices.ResumeLayout(false);
            this.groupDevices.PerformLayout();
            this.layoutDevices.ResumeLayout(false);
            this.layoutDevices.PerformLayout();
            this.layoutButtons.ResumeLayout(false);
            this.layoutButtons.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NotifyIcon NotifyIcon;
        private System.Windows.Forms.Timer IconTimer;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.GroupBox groupGeneral;
        private System.Windows.Forms.FlowLayoutPanel layoutGeneral;
        private System.Windows.Forms.CheckBox checkBoxStartup;
        private System.Windows.Forms.Label hintStartup;
        private System.Windows.Forms.CheckBox checkBoxNotification;
        private System.Windows.Forms.Label hintNotification;
        private System.Windows.Forms.FlowLayoutPanel layoutRefreshPeriod;
        private System.Windows.Forms.Label labelRefreshPeriod;
        private System.Windows.Forms.NumericUpDown numericUpDownRefreshPeriod;
        private System.Windows.Forms.Label labelRefreshUnit;
        private System.Windows.Forms.Label hintRefreshPeriod;
        private System.Windows.Forms.GroupBox groupDevices;
        private System.Windows.Forms.FlowLayoutPanel layoutDevices;
        private System.Windows.Forms.CheckBox checkBoxScanForEver;
        private System.Windows.Forms.Label hintScanForEver;
        private System.Windows.Forms.CheckBox checkBoxOneIconPerDevice;
        private System.Windows.Forms.Label hintOneIconPerDevice;
        private System.Windows.Forms.CheckBox checkBoxHideUnknownBattery;
        private System.Windows.Forms.Label hintHideUnknownBattery;
        private System.Windows.Forms.FlowLayoutPanel layoutButtons;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Button buttonAbout;
    }
}
