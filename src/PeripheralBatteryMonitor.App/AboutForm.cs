using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace PeripheralBatteryMonitor
{
    /// <summary>
    /// The About box: what this is, what version, and what it can read a battery from.
    /// Split out of the Settings window, which was carrying an "About" and a "Help"
    /// group box between the actual settings.
    /// </summary>
    internal sealed class AboutForm : Form
    {
        private const string ProjectUrl = "https://github.com/o0Zz/PeripheralBatteryMonitor";

            //Not derived from BatteryProviderRegistry: a provider is a way of reading a
            //battery, not a device family a user would recognise, and it carries no display
            //name. Keep this in step with the provider list in CLAUDE.md and the README.
        private static readonly string[] SupportedDevices =
        {
            "Bluetooth Low Energy — earbuds, fitness bands, BLE mice and keyboards",
            "Bluetooth Classic / BR-EDR — headsets, AirPods on Windows",
            "Apple Magic Mouse, Trackpad and Keyboard",
            "Logitech LIGHTSPEED devices on their own USB dongle",
            "SteelSeries Arctis Nova 5 / 7 wireless headsets",
        };

        internal AboutForm()
        {
            Text = "About Peripheral Battery Monitor";
            Icon = Properties.Resources.Icon_Battery_100;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

                //CenterScreen rather than CenterParent: the owner is the Settings form, which
                //spends most of its life hidden behind SetVisibleCore, and centering on an
                //invisible window puts this one in the top-left corner.
            StartPosition = FormStartPosition.CenterScreen;

            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            TableLayoutPanel body = new TableLayoutPanel();
            body.Dock = DockStyle.Fill;
            body.ColumnCount = 1;
            body.AutoSize = true;
            body.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            body.Padding = new Padding(16, 14, 16, 6);
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label name = new Label();
            name.Text = "Peripheral Battery Monitor";
            name.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
            name.AutoSize = true;
            name.Margin = new Padding(0, 0, 0, 2);

            Label version = new Label();
            version.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version;
            version.AutoSize = true;
            version.ForeColor = SystemColors.GrayText;
            version.Margin = new Padding(2, 0, 0, 12);

            Label what = Paragraph(
                "Shows how much battery your wireless peripherals have left, in the Windows " +
                "system tray. The icon follows the lowest level across every tracked device, " +
                "and a balloon notification fires once each time one drops to 20% or below.");
            what.Margin = new Padding(2, 0, 0, 12);

            Label supportedCaption = new Label();
            supportedCaption.Text = "Supported devices";
            supportedCaption.Font = new Font(Font, FontStyle.Bold);
            supportedCaption.AutoSize = true;
            supportedCaption.Margin = new Padding(2, 0, 0, 4);

            Label supported = Paragraph("• " + string.Join("\n• ", SupportedDevices));
            supported.Margin = new Padding(10, 0, 0, 14);

            LinkLabel link = new LinkLabel();
            link.Text = "by o0Zz — " + ProjectUrl;
            link.AutoSize = true;
            link.Margin = new Padding(2, 0, 0, 0);
            link.LinkArea = new LinkArea(link.Text.IndexOf("https", StringComparison.Ordinal), ProjectUrl.Length);
            link.LinkClicked += OpenProjectPage;

            Button ok = new Button();
            ok.Text = "OK";
            ok.AutoSize = true;
            ok.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ok.MinimumSize = new Size(84, 26);
            ok.DialogResult = DialogResult.OK;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttons.Padding = new Padding(8, 6, 14, 10);
            buttons.Controls.Add(ok);

            Control[] rows = { name, version, what, supportedCaption, supported, link };
            foreach (Control row in rows)
            {
                body.RowCount++;
                body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                body.Controls.Add(row);
            }

                //Fill first, Bottom second: docking is applied from the highest index down, so
                //the filled panel has to sit at index 0 to be laid out last and take what is left.
            Controls.Add(body);
            Controls.Add(buttons);

            AcceptButton = ok;
            CancelButton = ok;
        }

            //MaximumSize with a zero height is what turns AutoSize into "wrap at this width and
            //grow downwards"; WinForms scales MaximumSize with the rest of the form, so the
            //wrap point follows the display scale.
        private static Label Paragraph(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.MaximumSize = new Size(420, 0);
            label.Margin = new Padding(2, 0, 0, 4);
            return label;
        }

        private void OpenProjectPage(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(ProjectUrl);
                ((LinkLabel)sender).LinkVisited = true;
            }
            catch (Exception ex)
            {
                    //No browser, or the shell refused. Worth saying so once, not worth crashing
                    //the tray app over.
                MessageBox.Show(this, ProjectUrl + "\n\n" + ex.Message, "Could not open the page",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
