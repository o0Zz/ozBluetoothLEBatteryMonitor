using PeripheralBatteryMonitor;
using System;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace PeripheralBatteryMonitor
{
    public partial class Info : Form
    {
        private DeviceManager deviceManager;
        private Func<bool> hideUnknownBattery;

        public Info(DeviceManager deviceManager, Func<bool> hideUnknownBattery)
        {
            InitializeComponent();
            this.deviceManager = deviceManager;
            this.hideUnknownBattery = hideUnknownBattery;
        }

            //Columns are built here rather than in the constructor because ListView
            //columns are the one thing AutoScaleMode.Font does not scale: their widths
            //are plain integers the control never revisits. By OnLoad the form has been
            //auto-scaled, so ClientSize and DeviceDpi are the real ones and the two
            //fixed columns can be scaled to match.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (listView1.Columns.Count != 0)
                return;

            int fixedColumn = Scale(100);
            listView1.Columns.Add("Device", listView1.ClientSize.Width - (2 * fixedColumn) - Scale(5));
            listView1.Columns.Add("State", fixedColumn);
            listView1.Columns.Add("Battery Level", fixedColumn);
        }

            //96 is the DPI the designer laid this form out at -- see AutoScaleDimensions.
        private int Scale(int designPixels)
        {
            return (int)Math.Round(designPixels * (this.DeviceDpi / 96.0));
        }

        public new void Show()
        {
            base.Show();

            this.Cursor = new Cursor(Cursor.Current.Handle);

            this.Left = Cursor.Position.X - this.Width;
            this.Top = Cursor.Position.Y - this.Height - 20;
            
            this.Activate();
        }

        private void Info_Deactivate(object sender, EventArgs e)
        {
            Hide();
        }

        private void Info_Activated(object sender, EventArgs e)
        {
            DateTime ?lastUpdated = null;

            listView1.BeginUpdate();
            listView1.Items.Clear();

            ConcurrentDictionary<string, BatteryDevice> deviceDict = deviceManager.getDeviceList();

            foreach (BatteryDevice device in deviceDict.Values)
            {
                int theBatteryLevel = device.GetBatteryLevel();
                string theName = device.GetName();
                lastUpdated = device.GetLastUpdatedTime();

                if (theBatteryLevel < 0 && hideUnknownBattery != null && hideUnknownBattery())
                    continue;

                ListViewItem listViewItem = new ListViewItem
                {
                    Text = device.GetName()
                };
                listViewItem.SubItems.Add(device.IsConnected() ? "Connected" : "Disconnected");
                listViewItem.SubItems.Add(device.GetBatteryLevel() + "%");
                listViewItem.Tag = device;
                listView1.Items.Add(listViewItem);

            }

            listView1.EndUpdate();

            toolStripStatusLabel2.Text = lastUpdated.ToString();
        }
    }
}
