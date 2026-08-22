namespace PeripheralBatteryMonitor
{
    partial class Info
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripRefresh = new System.Windows.Forms.ToolStripButton();
            this.listView1 = new System.Windows.Forms.ListView();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripRefresh});
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(102, 20);
            this.toolStripStatusLabel1.Text = "Last updated: ";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(15, 20);
            this.toolStripStatusLabel2.Text = "-";
            // 
            // toolStripRefresh
            //
            // On the strip the form already has, right-aligned, rather than a real Button in
            // a new docked panel: the strip is the only chrome here, and a ToolStripItem
            // auto-sizes to its text, so a long translation widens the item instead of being
            // clipped by a designer-written width.
            this.toolStripRefresh.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripRefresh.Name = "toolStripRefresh";
            this.toolStripRefresh.Text = "Refresh";
            this.toolStripRefresh.Click += new System.EventHandler(this.toolStripRefresh_Click);
            // 
            // listView1
            // 
            this.listView1.HideSelection = false;
            // Docked, not placed. This used to be pinned at (0,-3) with a fixed 416x143 --
            // the negative Y hid its top border and the height was hand-fitted to leave room
            // for the status strip, landing 2 px short of it. Neither survives the window
            // being resized, because a control with no Dock and no Anchor simply does not
            // move. Fill also removes the gap and the need for the offset.
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            // No border: the list is the whole window here, so a sunken frame inside a frame
            // reads as a mistake. This is what the -3 offset was approximating.
            this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listView1.Name = "listView1";
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            //
            // Info
            //
            // Same 96 DPI baseline the Settings form declares. This pair was missing
            // entirely, which left the popup at AutoScaleMode.None: it kept its
            // design-time pixel size while the font grew with the display scale, so
            // above 100% the rows were clipped by the status strip.
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 168);
            this.ControlBox = false;
            // Fill first, Bottom second: docking is applied from the highest index down, so
            // the status strip claims its band and the list takes what is left.
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.statusStrip1);
            // Small enough to be useful, large enough that the two fixed columns plus a
            // readable device column still fit. Scaled with the rest of the form.
            this.MinimumSize = new System.Drawing.Size(320, 140);
            this.Icon = global::PeripheralBatteryMonitor.Properties.Resources.Icon_Battery_100;
            this.Name = "Info";
            this.Activated += new System.EventHandler(this.Info_Activated);
            this.Deactivate += new System.EventHandler(this.Info_Deactivate);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripButton toolStripRefresh;
    }
}
