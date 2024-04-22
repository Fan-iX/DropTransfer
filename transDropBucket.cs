using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using ShellApp;
using System.Reflection;


namespace DropTransfer
{
    public partial class transDropBucket : Form
    {
        private BucketTabControl tc = new BucketTabControl()
        {
            Dock = DockStyle.Fill,
            SizeMode = TabSizeMode.Normal,
            TabStop = false
        };

        private Size fullSize;
        private DpiFactor dpiScale;

        private bool allowFold = false;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            fullSize = new Size(this.ClientSize.Width, Math.Max(this.ClientSize.Height, 150 * dpiScale));
            Properties.Settings.Default.FullSize = this.fullSize;
            Properties.Settings.Default.WindowLocation = this.Location;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }

        protected void ResizeControl()
        {
            Global.imgList.ImageSize = new Size(
                16 * this.dpiScale,
                16 * this.dpiScale
            );
            Global.imgList.Images.Clear();
            foreach (BucketTabPage tp in tc.TabPages)
            {
                BucketListView lv = tp.BucketListView as BucketListView;
                foreach (ListViewItem item in lv.Items)
                {
                    string path = item.ImageKey;
                    if (!Global.imgList.Images.ContainsKey(item.ImageKey))
                        Global.imgList.Images.Add(item.ImageKey, ShellInfoHelper.GetIconFromPath(item.ImageKey));
                }
            }
            BucketTabPage tpSelected = tc.SelectedTab as BucketTabPage;
            if (tpSelected != null)
                tpSelected.BucketListView.Refresh();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            this.dpiScale = new DpiFactor(e.DeviceDpiNew / 96.0f);
            if (tc.Visible) this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            ResizeControl();
        }

        protected override void WndProc(ref Message m)
        {
            // const int WM_NCLBUTTONDOWN = 0x00A1;
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_NCRBUTTONDOWN = 0x00A4;
            const int WM_CONTEXTMENU = 0x007B;
            switch (m.Msg)
            {
                case WM_CONTEXTMENU:
                    m.Result = IntPtr.Zero;
                    break;
                case WM_NCLBUTTONDBLCLK:
                case WM_NCRBUTTONDOWN:
                    allowFold = !allowFold;
                    if (allowFold)
                        this.FoldWindow();
                    else
                        this.UnfoldWindow();
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        public transDropBucket()
        {
            Text = "拖放中转站 [自动]";
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(transDropBucket));
            fullSize = Properties.Settings.Default.FullSize;

            this.AllowDrop = true;
            this.ClientSize = fullSize;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.WindowsDefaultLocation;
            this.TopMost = true;
            this.KeyPreview = true;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.dpiScale = new DpiFactor(this.DeviceDpi / 96.0f);
            this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            this.Font = SystemFonts.CaptionFont;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DropTransfer.Resources.DropTransfer.ico"))
            {
                this.Icon = new Icon(stream);
            }

            this.DragEnter += new DragEventHandler(form_DragEnter);
            this.DragLeave += new EventHandler(form_DragLeave);
            this.ResizeEnd += new EventHandler(form_ResizeEnd);
            this.KeyDown += new KeyEventHandler(form_KeyDown);
            this.Resize += new EventHandler(form_Resize);
            this.Load += new EventHandler(form_Load);

            this.Controls.Add(tc);
            this.ResumeLayout(false);

            ResizeControl();
        }

        private void form_Resize(object sender, EventArgs e)
        {
            BucketTabPage tp = tc.SelectedTab as BucketTabPage;
            ListView lv = tp.BucketListView;
            lv.Columns[0].Width = lv.ClientSize.Width - lv.Columns[1].Width - lv.Columns[2].Width - lv.Columns[3].Width;
        }

        public void FoldWindow()
        {
            tc.Hide();
            this.MinimumSize = new Size(150 * dpiScale, 0);
            this.MaximumSize = new Size(int.MaxValue, 0);
            this.ClientSize = new Size(fullSize.Width, 0);
        }

        public void UnfoldWindow()
        {
            tc.Show();
            this.MaximumSize = new Size(0, 0);
            this.ClientSize = fullSize;
            this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
        }

        private void form_ResizeEnd(object sender, EventArgs e)
        {
            if (tc.Visible) fullSize = this.ClientSize;
        }

        private void form_DragEnter(object sender, DragEventArgs e)
        {
            if (allowFold)
                UnfoldWindow();
        }

        private void form_DragLeave(object sender, EventArgs e)
        {
            if (allowFold & !this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition)))
                FoldWindow();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.W)
            {
                Application.Exit();
            }
            else if (e.Control && e.KeyCode == Keys.E)
            {
                this.Location = new Point(Cursor.Position.X - this.ClientSize.Width / 2, Cursor.Position.Y - this.ClientSize.Height / 2);
            }
        }

        private void form_Load(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.WindowLocation;
            this.ClientSize = Properties.Settings.Default.FullSize;
        }

        public void OnMouseEnterWindow()
        {
            if (allowFold) UnfoldWindow();
        }

        public void OnMouseLeaveWindow()
        {
            if (allowFold) FoldWindow();
        }
    }
}
