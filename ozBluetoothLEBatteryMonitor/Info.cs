﻿using BluetoothLEBatteryMonitor.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Windows.Foundation;

namespace BluetoothLEBatteryMonitor
{
    public partial class Info : Form
    {
        private DeviceManager deviceManager;

        public Info(DeviceManager deviceManager)
        {
            InitializeComponent();
            this.deviceManager = deviceManager;

            listView1.Columns.Add("Device", listView1.Width - 205);
            listView1.Columns.Add("State", 100);
            listView1.Columns.Add("Battery Level", 100);
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

            ConcurrentDictionary<string, DeviceBLE> deviceDict = deviceManager.getDeviceList();

            foreach (DeviceBLE device in deviceDict.Values)
            {
                int theBatteryLevel = device.GetBatteryLevel();
                string theName = device.GetName();
                lastUpdated = device.GetLastUpdatedTime();

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

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
