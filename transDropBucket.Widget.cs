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
using ShellApp;

namespace DropTransfer
{
    public class Global
    {
        public static ShellContextMenu ctxMnu = new ShellContextMenu();
        public static DragDropEffects dragEffect = DragDropEffects.All;
        public static ImageList imgList = new ImageList();
    }

    public class ListViewWithoutHorizontalScrollBar : ListView
    {
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_STYLE = -16;
        const int WM_NCCALCSIZE = 0x83;
        const int WS_VSCROLL = 0x200000;
        const int WS_HSCROLL = 0x100000;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCCALCSIZE:
                    int style = (int)GetWindowLongPtr64(this.Handle, GWL_STYLE);
                    if ((style & WS_HSCROLL) == WS_HSCROLL)
                        style &= ~WS_HSCROLL;
                    SetWindowLongPtr64(this.Handle, GWL_STYLE, style);
                    base.WndProc(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }

    public class TabControlWithoutMinWidth : TabControl
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Send TCM_SETMINTABWIDTH
            SendMessage(this.Handle, 0x1331, IntPtr.Zero, (IntPtr)10);
        }
    }

    public class BucketListView : ListViewWithoutHorizontalScrollBar
    {
        public BucketListView()
        {
            AllowDrop = true;
            CheckBoxes = true;

            View = View.Details;
            SmallImageList = Global.imgList;
            ListViewItemSorter = new FileListViewColumnSorter();
            Columns.Add("文件", -2, HorizontalAlignment.Left);
            Columns.Add("类型", -2, HorizontalAlignment.Left);
            Columns.Add("修改时间", -2, HorizontalAlignment.Left);
            Columns.Add("大小", -2, HorizontalAlignment.Right);

            DragOver += new DragEventHandler((object sender, DragEventArgs e) => Focus());
            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            });
            ItemDrag += new ItemDragEventHandler((object sender, ItemDragEventArgs e) =>
            {
                List<string> paths = new List<string>();
                foreach (ListViewItem item in SelectedItems)
                    paths.Add(item.Name);
                DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                DoDragDrop(fileData, Global.dragEffect);
            });
            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddItems(paths);
            });

            KeyDown += new KeyEventHandler((object sender, KeyEventArgs e) =>
            {
                if (e.Control && e.KeyCode == Keys.V)
                {
                    StringCollection dropList = Clipboard.GetFileDropList();
                    if (dropList == null || dropList.Count == 0) return;
                    AddItems(dropList);
                }
                else if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.X))
                {
                    StringCollection dropList = new StringCollection();
                    foreach (ListViewItem item in SelectedItems)
                        dropList.Add(item.Name);
                    DataObject data = new DataObject();
                    data.SetFileDropList(dropList);
                    Clipboard.SetDataObject(data);
                    if (e.KeyCode == Keys.X)
                    {
                        foreach (ListViewItem item in SelectedItems)
                            item.Remove();
                    }
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    foreach (ListViewItem item in SelectedItems)
                        item.Remove();
                }
            });

            MouseClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (FocusedItem != null && FocusedItem.Bounds.Contains(e.Location))
                    {
                        List<string> paths = new List<string>();
                        foreach (ListViewItem item in SelectedItems)
                            paths.Add(item.Name);
                        Global.ctxMnu.ShowContextMenu(paths.Select(x => new FileInfo(x)).ToArray(), this.PointToScreen(new Point(e.X, e.Y)));
                    }
                }
            });
            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                ListViewItem item = FocusedItem;
                if (e.Button == MouseButtons.Left && (File.Exists(item.Name) || Directory.Exists(item.Name)))
                {
                    Process.Start(new ProcessStartInfo(item.Name) { UseShellExecute = true });
                }
            });
            ColumnClick += new ColumnClickEventHandler((object sender, ColumnClickEventArgs e) =>
            {
                FileListViewColumnSorter columnSorter = ListViewItemSorter as FileListViewColumnSorter;
                if (e.Column == columnSorter.SortColumn)
                {
                    if (columnSorter.Order == SortOrder.Ascending)
                        columnSorter.Order = SortOrder.Descending;
                    else
                        columnSorter.Order = SortOrder.Ascending;
                }
                else
                {
                    columnSorter.SortColumn = e.Column;
                    columnSorter.Order = SortOrder.Ascending;
                }
                Sort();
            });
        }

        override public void Refresh()
        {
            base.Refresh();
            AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
            AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);
            Columns[0].Width = ClientSize.Width - Columns[1].Width - Columns[2].Width - Columns[3].Width;
        }

        public void AddItems(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            BeginUpdate();
            foreach (string path in paths)
                AddItem(path);
            EndUpdate();
            Refresh();
            Parent.Text = Items.Count.ToString();
        }

        public void AddItems(StringCollection paths)
        {
            if (paths == null || paths.Count == 0) return;
            BeginUpdate();
            foreach (string path in paths)
                AddItem(path);
            EndUpdate();
            Refresh();
            Parent.Text = Items.Count.ToString();
        }

        public void AddItem(string sourceName)
        {
            if (Items.ContainsKey(sourceName)) return;
            if (Directory.Exists(sourceName))
                Items.Add(new ListViewDirectoryItem(sourceName));
            else if (File.Exists(sourceName))
                Items.Add(new ListViewFileItem(sourceName));
        }
    }

    public class FileListViewColumnSorter : IComparer
    {
        public int SortColumn;
        public SortOrder Order;
        public FileListViewColumnSorter()
        {
            SortColumn = 1;
            Order = SortOrder.None;
        }
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;
            CaseInsensitiveComparer ObjectCompare = new CaseInsensitiveComparer();
            if (SortColumn == 3)
            {
                compareResult = ObjectCompare.Compare(
                    listviewX.SubItems[SortColumn].Tag,
                    listviewY.SubItems[SortColumn].Tag
                );
            }
            else
            {
                compareResult = ObjectCompare.Compare(
                    listviewX.SubItems[SortColumn].Text,
                    listviewY.SubItems[SortColumn].Text
                );
            }
            if (SortColumn == 2) compareResult = -compareResult;
            if (Order == SortOrder.Ascending)
                return compareResult;
            else if (Order == SortOrder.Descending)
                return -compareResult;
            else
                return 0;
        }
    }

    class ListViewDirectoryItem : ListViewItem
    {
        private FileSystemWatcher watcher;

        public ListViewDirectoryItem(string path)
        {
            Name = path;
            string ext = File.Exists(path) ? Path.GetExtension(path) : "";
            if (!Global.imgList.Images.ContainsKey(path))
            {
                Global.imgList.Images.Add(path, ShellInfoHelper.GetIconFromPath(path));
            }
            ImageKey = path;
            Text = ShellInfoHelper.GetDisplayNameFromPath(path);
            SubItems.Add(ext);
            SubItems.Add(File.GetLastWriteTime(path).ToString("yyyy/MM/dd hh:mm"));
            SubItems.Add(""); // size
            SubItems[3].Tag = (long)0;

            watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path),
                IncludeSubdirectories = true
            };
            watcher.Changed += new FileSystemEventHandler(OnFileChanged);
            watcher.Deleted += new FileSystemEventHandler((object sender, FileSystemEventArgs e) => Remove());
            watcher.Renamed += new RenamedEventHandler((object sender, RenamedEventArgs e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    Name = e.FullPath;
                    SubItems[0].Text = e.Name;
                    watcher.Filter = Path.GetFileName(e.FullPath);
                }
            });
            watcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            SubItems[2].Text = File.GetLastWriteTime(Name).ToString("yyyy/MM/dd hh:mm");
        }

        override public void Remove()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            ListView.Parent.Text = (ListView.Items.Count - 1).ToString();
            base.Remove();
        }
    }

    class ListViewFileItem : ListViewDirectoryItem
    {
        private FileInfo fileInfo;
        public ListViewFileItem(string path) : base(path)
        {
            fileInfo = new FileInfo(path);

            SubItems[3].Text = FileSizeHelper.GetHumanReadableFileSize(fileInfo.Length);
            SubItems[3].Tag = (long)fileInfo.Length;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                SubItems[2].Text = File.GetLastWriteTime(Name).ToString("yyyy/MM/dd hh:mm");
                SubItems[3].Text = FileSizeHelper.GetHumanReadableFileSize(fileInfo.Length);
                SubItems[3].Tag = (long)fileInfo.Length;
            }
        }
    }

    public class BucketTabPage : TabPage
    {
        public BucketListView BucketListView;

        public BucketTabPage()
        {
            Text = "0";
            BucketListView = new BucketListView()
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(BucketListView);
            ContextMenuStrip = new ContextMenuStrip()
            {
                DropShadowEnabled = false
            };

            ContextMenuStrip.Items.Add("全选").Click += new EventHandler((object sender, EventArgs e) =>
            {
                foreach (ListViewItem item in BucketListView.Items)
                {
                    item.Checked = true;
                    item.Selected = true;
                }
            });

            ContextMenuStrip.Items.Add("移除选中").Click += new EventHandler((object sender, EventArgs e) =>
            {
                foreach (ListViewItem item in BucketListView.CheckedItems)
                    item.Remove();
            });

            ContextMenuStrip.Items.Add("切换拖放模式").Click += new EventHandler((object sender, EventArgs e) =>
            {
                if (Global.dragEffect == DragDropEffects.All)
                {
                    Global.dragEffect = DragDropEffects.Copy;
                    FindForm().Text = "拖放中转站 [复制]";
                }
                else if (Global.dragEffect == DragDropEffects.Copy)
                {
                    Global.dragEffect = DragDropEffects.Move;
                    FindForm().Text = "拖放中转站 [移动]";
                }
                else if (Global.dragEffect == DragDropEffects.Move)
                {
                    Global.dragEffect = DragDropEffects.All;
                    FindForm().Text = "拖放中转站 [自动]";
                }
            });
        }
    }

    public class BucketTabControl : TabControlWithoutMinWidth
    {
        private BucketTabPage draggedTab = null;
        private BucketTabPage contextTab = null;
        private BucketTabPage tpPlus = new BucketTabPage()
        {
            Text = "+"
        };
        private ContextMenuStrip tpCtxMnu = new ContextMenuStrip()
        {
            DropShadowEnabled = false
        };

        public BucketTabControl()
        {
            Dock = DockStyle.Fill;
            AllowDrop = true;

            TabPages.Add(new BucketTabPage());
            TabPages.Add(tpPlus);

            DragEventHandler tpDragDropExtra = null;
            tpDragDropExtra = new DragEventHandler((object sender, DragEventArgs e) =>
            {
                ListView lv = sender as ListView;
                if (lv.Items.Count != 0)
                {
                    tpPlus.BucketListView.DragDrop -= tpDragDropExtra;
                    tpPlus = new BucketTabPage() { Text = "+" };
                    tpPlus.BucketListView.DragDrop += tpDragDropExtra;
                    TabPages.Add(tpPlus);
                }
            });
            tpPlus.BucketListView.DragDrop += tpDragDropExtra;

            SelectedIndexChanged += new EventHandler((object sender, EventArgs e) =>
            {
                BucketTabPage tp = SelectedTab as BucketTabPage;
                tp.BucketListView.Refresh();
            });

            tpCtxMnu.Items.Add("新建页").Click += new EventHandler((object sender, EventArgs e) =>
            {
                TabPages.Insert(TabPages.Count - 1, new BucketTabPage());
                SelectedTab = TabPages[TabPages.Count - 2];
            });
            tpCtxMnu.Items.Add("删除页").Click += new EventHandler((object sender, EventArgs e) =>
            {
                if (contextTab == SelectedTab)
                {
                    int index = TabPages.IndexOf(contextTab) - 1;
                    if (index < 0) index = 0;
                    SelectedTab = TabPages[index];
                }
                TabPages.Remove(contextTab);
            });

            MouseDown += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                BucketTabPage tp = getPointedTab() as BucketTabPage;
                if (e.Button == MouseButtons.Left)
                {
                    if (tp == tpPlus)
                    {
                        TabPages.Insert(TabPages.Count - 1, new BucketTabPage());
                        SelectedTab = TabPages[TabPages.Count - 2];
                    }
                    else if (tp != null)
                    {
                        draggedTab = tp;
                        List<string> paths = new List<string>();
                        foreach (ListViewItem item in draggedTab.BucketListView.Items)
                            paths.Add(item.Name);
                        if (paths.Count > 0)
                        {
                            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                            DoDragDrop(fileData, Global.dragEffect);
                        }
                        else
                        {
                            DoDragDrop(draggedTab, DragDropEffects.Move);
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    if (tp != null && tp != tpPlus)
                    {
                        contextTab = tp;
                        tpCtxMnu.Show(this, e.Location);
                    }
                }
            });

            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                BucketTabPage tp = getPointedTab() as BucketTabPage;
                if (tp != null && tp != SelectedTab)
                {
                    SelectedTab = tp;
                }
                if (draggedTab != null)
                {
                    if (tp != null)
                    {
                        e.Effect = DragDropEffects.All;
                    }
                }
                else
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                        e.Effect = DragDropEffects.All;
                }
            });

            QueryContinueDrag += new QueryContinueDragEventHandler((object sender, QueryContinueDragEventArgs e) =>
            {
                if ((e.Action & DragAction.Drop) == DragAction.Drop)
                {
                    if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                        draggedTab = null;
                }
            });

            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                BucketTabPage targetTab = getPointedTab() as BucketTabPage;
                if (targetTab == tpPlus)
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (paths.Length == 0) return;
                    targetTab.BucketListView.AddItems(paths);
                    tpPlus.BucketListView.DragDrop -= tpDragDropExtra;
                    tpPlus = new BucketTabPage() { Text = "+" };
                    tpPlus.BucketListView.DragDrop += tpDragDropExtra;
                    TabPages.Add(tpPlus);
                }
                else if (draggedTab != null)
                {
                    if (targetTab == null || targetTab == draggedTab) return;
                    int targetIndex = TabPages.IndexOf(targetTab);
                    TabPages.Remove(draggedTab);
                    TabPages.Insert(targetIndex, draggedTab);
                    SelectedTab = draggedTab;
                    draggedTab = null;
                }
                else
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    targetTab.BucketListView.AddItems(paths);
                }
            });

        }

        private TabPage getPointedTab()
        {
            Point pt = PointToClient(Cursor.Position);
            for (int i = 0; i < TabPages.Count; i++)
                if (GetTabRect(i).Contains(pt))
                    return TabPages[i];
            return null;
        }
    }

}
