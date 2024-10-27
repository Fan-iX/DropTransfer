using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using ShellApp;
using System.Timers;


namespace DropTransfer
{
    public class Global
    {
        public static ShellContextMenu ctxMnu = new ShellContextMenu();
        public static ImageList imgList = new ImageList();
        public static DateTime lastLayoutLock = new DateTime(0);
        public static System.Timers.Timer layoutLockTimer = new System.Timers.Timer()
        {
            Interval = 100
        };
    }

    public class Consts
    {
        public static readonly string[] imageExts = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".ico", ".tif", ".tiff" };
        public static readonly Dictionary<DragDropEffects, string> dragEffectDict = new Dictionary<DragDropEffects, string>()
        {
            { DragDropEffects.All, "自动" },
            { DragDropEffects.Copy, "复制" },
            { DragDropEffects.Move, "移动" }
        };
    }

    public class transDropBucket : PocketForm
    {
        private BucketTabControl tc = new BucketTabControl()
        {
            Dock = DockStyle.Fill,
            SizeMode = TabSizeMode.Normal,
            TabStop = false
        };

        public bool UseThumbnail
        {
            get => Properties.Settings.Default.UseThumbnail;
            set
            {
                Properties.Settings.Default.UseThumbnail = value;
                ResizeControl();
            }
        }

        public int IconSize
        {
            get => Properties.Settings.Default.IconSize;
            set
            {
                Properties.Settings.Default.IconSize = value;
                Global.imgList.ImageSize = new Size(value * DpiScale, value * DpiScale);
                ResizeControl();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            IEnumerable<BucketTabPage> tabs = tc.TabPages.Cast<BucketTabPage>();
            Properties.Settings.Default.History = tabs.Where(x => x.BucketItems.Count > 0).Select(
                x => new ValueTuple<string, List<string>>(x.Name, x.BucketItems.Cast<ListViewItem>().Select(y => y.Name).ToList())
            ).ToList();
            int index = tc.SelectedIndex - tabs.Take(tc.SelectedIndex + 1).Where(x => x.BucketItems.Count == 0).Count();
            if (index < 0) index = 0;
            Properties.Settings.Default.SelectedIndex = index;
            Properties.Settings.Default.UnfoldedSize = new Size(ClientSize.Width, Math.Max(UnfoldedSize.Height, 150 * DpiScale));
            Properties.Settings.Default.WindowLocation = Location;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }

        private void ResizeControl()
        {
            Global.imgList.Images.Clear();
            foreach (BucketTabPage tp in tc.TabPages)
            {
                BucketListView lv = tp.BucketListView as BucketListView;
                foreach (ListViewItem item in lv.Items)
                {
                    string path = item.Name;
                    string ImageKey = item.ImageKey;

                    if (!Global.imgList.Images.ContainsKey(ImageKey))
                    {
                        if (Properties.Settings.Default.UseThumbnail && File.Exists(path) && Consts.imageExts.Contains(Path.GetExtension(path).ToLower()))
                        {
                            Image img = Image.FromFile(path);
                            Global.imgList.Images.Add(ImageKey, img.GetThumbnailImage(128, 128, () => false, IntPtr.Zero));
                            img.Dispose();
                        }
                        else
                            Global.imgList.Images.Add(ImageKey, ShellInfoHelper.GetIconFromPath(path, IconSize == 16));
                    }
                    item.ImageKey = item.ImageKey;
                }
            }
            BucketTabPage tpSelected = tc.SelectedTab as BucketTabPage;
            if (tpSelected != null)
                tpSelected.BucketListView.Refresh();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            IconSize = IconSize;
        }

        public transDropBucket()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(transDropBucket));
            UnfoldedSize = Properties.Settings.Default.UnfoldedSize;
            Text = "拖放中转站";

            ClientSize = UnfoldedSize;
            MinimumSize = new Size(150 * DpiScale, 150 * DpiScale);
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DropTransfer.Resources.DropTransfer.ico"))
            {
                Icon = new Icon(stream);
            }

            KeyDown += new KeyEventHandler((object sender, KeyEventArgs e) =>
            {
                if (e.Control && e.KeyCode == Keys.W)
                {
                    Application.Exit();
                }
                else if (e.Control && e.KeyCode == Keys.E)
                {
                    Location = new Point(Cursor.Position.X - this.ClientSize.Width / 2, Cursor.Position.Y - this.ClientSize.Height / 2);
                }
            });
            Resize += new EventHandler((object sender, EventArgs e) =>
            {
                BucketTabPage tp = tc.SelectedTab as BucketTabPage;
                ListView lv = tp.BucketListView;
                lv.Columns[0].Width = lv.ClientSize.Width - lv.Columns[1].Width - lv.Columns[2].Width - lv.Columns[3].Width;
            });
            Load += new EventHandler((object sender, EventArgs e) =>
            {
                Location = Properties.Settings.Default.WindowLocation;
                ClientSize = Properties.Settings.Default.UnfoldedSize;
            });
            List<(string, List<string>)> history = Properties.Settings.Default.History;
            int index = Properties.Settings.Default.SelectedIndex;

            if (history.Count > 0)
            {
                tc.CreatePages(history);
                if (index >= 0 && index < tc.TabPages.Count)
                    tc.SelectedIndex = index;
            }
            else
            {
                IntPtr h = tc.Handle;
                BucketTabPage tp = new BucketTabPage();
                tc.TabPages.Insert(0, tp);
                tc.SelectedIndex = 0;
            }
            Controls.Add(tc);
            ResumeLayout(false);

            IconSize = Properties.Settings.Default.IconSize;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }



    public class BucketListView : ListViewWithoutHorizontalScrollBar
    {
        public transDropBucket Form { get => FindForm() as transDropBucket; }

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

            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                SelectedItems.Clear();
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.All;
            });
            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Focus();
                if (e.Effect == DragDropEffects.None) return;
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                if (item == null) return;
                InsertionMark.AppearsAfterItem = point.Y > item.Bounds.Top + item.Bounds.Height / 2;
                InsertionMark.Index = item.Index;
            });
            DragLeave += new EventHandler((object sender, EventArgs e) =>
            {
                InsertionMark.Index = -1;
            });
            ItemDrag += new ItemDragEventHandler((object sender, ItemDragEventArgs e) =>
            {
                List<string> paths = new List<string>();
                foreach (ListViewItem item in SelectedItems)
                    paths.Add(item.Name);
                DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                DoDragDrop(fileData, Properties.Settings.Default.DragEffect);
            });
            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                    if (item == null || point.Y > item.Bounds.Top + item.Bounds.Height / 2)
                        InsertFileSystemItemsAfter(item, paths);
                    else
                        InsertFileSystemItemsBefore(item, paths);
                }
                InsertionMark.Index = -1;
            });

            KeyDown += new KeyEventHandler((object sender, KeyEventArgs e) =>
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    foreach (ListViewItem item in Items)
                    {
                        item.Checked = true;
                        item.Selected = true;
                    }
                }
                if (e.Control && e.KeyCode == Keys.V)
                {
                    StringCollection dropList = Clipboard.GetFileDropList();
                    if (dropList == null || dropList.Count == 0) return;
                    AddFileSystemItems(dropList);
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
                        Global.ctxMnu.ShowContextMenu(paths.Select(x => new FileInfo(x)).ToArray(), Cursor.Position);
                    }
                }
            });
            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                ListViewItem item = FocusedItem;
                if (e.Button == MouseButtons.Left)
                {
                    if (File.Exists(item.Name))
                    {
                        Process.Start(new ProcessStartInfo(item.Name) { UseShellExecute = true });
                    }
                    else if (Directory.Exists(item.Name))
                    {
                        if (Properties.Settings.Default.UseNavigate)
                        {
                            ShellApplicationHelper.NavigateExplorerTo(item.Name);
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo(item.Name) { UseShellExecute = true });
                        }
                    }
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

        public void AddItem(ListViewItem item)
        {
            ListViewItem old_item = Items[item.Name];
            if (Items.Contains(item)) item.Remove();
            Items.Add(item);
            if (old_item != null && old_item != item) old_item.Remove();
        }

        public ListViewItem InsertItemBefore(ListViewItem anchor, ListViewItem item)
        {
            ListViewItem old_item = Items[item.Name];
            if (anchor != null && anchor == item) return item;
            if (Items.Contains(item)) Items.Remove(item);

            if (anchor == null || !Items.Contains(anchor))
                Items.Insert(0, item);
            else
                Items.Insert(Items.IndexOf(anchor), item);
            if (old_item != null && old_item != item) old_item.Remove();

            item.Selected = true;
            item.Focused = true;
            return item;
        }

        public ListViewItem InsertItemAfter(ListViewItem anchor, ListViewItem item)
        {
            ListViewItem old_item = Items[item.Name];
            if (anchor != null && anchor == item) return item;

            if (Items.Contains(item)) Items.Remove(item);
            if (anchor == null || !Items.Contains(anchor))
                Items.Add(item);
            else
                Items.Insert(Items.IndexOf(anchor) + 1, item);
            if (old_item != null && old_item != item) old_item.Remove();

            item.Selected = true;
            item.Focused = true;
            return item;
        }

        public void AddItems(ListViewItem[] items)
        {
            if (items == null || items.Length == 0) return;
            BeginUpdate();
            foreach (ListViewItem item in items)
                AddItem(item);
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void InsertItemsBefore(ListViewItem anchor, ListViewItem[] items)
        {
            if (items == null || items.Length == 0) return;
            BeginUpdate();
            SelectedItems.Clear();
            foreach (ListViewItem item in items.Reverse())
            {
                anchor = InsertItemBefore(anchor, item);
                anchor.Selected = true;
            }
            anchor.Focused = true;
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void InsertItemsAfter(ListViewItem anchor, ListViewItem[] items)
        {
            if (items == null || items.Length == 0) return;
            BeginUpdate();
            SelectedItems.Clear();
            foreach (ListViewItem item in items)
            {
                anchor = InsertItemAfter(anchor, item);
                anchor.Selected = true;
            }
            anchor.Focused = true;
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void AddFileSystemItems(string[] paths)
        {
            ListViewItem[] items = paths.Select(x => Items.ContainsKey(x) ? Items[x] :
                Directory.Exists(x) ? new ListViewDirectoryItem(x) as ListViewItem :
                File.Exists(x) ? new ListViewFileItem(x) as ListViewItem : null).ToArray();
            AddItems(items);
        }

        public void AddFileSystemItems(List<string> paths)
        {
            AddFileSystemItems(paths.ToArray());
        }

        public void AddFileSystemItems(StringCollection paths)
        {
            AddFileSystemItems(paths.Cast<string>().ToArray());
        }

        public void InsertFileSystemItemsBefore(ListViewItem anchor, string[] paths)
        {
            ListViewItem[] items = paths.Select(x => Items.ContainsKey(x) ? Items[x] :
                Directory.Exists(x) ? new ListViewDirectoryItem(x) as ListViewItem :
                File.Exists(x) ? new ListViewFileItem(x) as ListViewItem : null).ToArray();
            InsertItemsBefore(anchor, items);
        }

        public void InsertFileSystemItemsAfter(ListViewItem anchor, string[] paths)
        {
            ListViewItem[] items = paths.Select(x => Items.ContainsKey(x) ? Items[x] :
                Directory.Exists(x) ? new ListViewDirectoryItem(x) as ListViewItem :
                File.Exists(x) ? new ListViewFileItem(x) as ListViewItem : null).ToArray();
            InsertItemsAfter(anchor, items);
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

    class ListViewFileSystemItem : ListViewItem
    {
        internal FileSystemWatcher watcher;
        public ListViewFileSystemItem(string path)
        {
            Name = path;
            string ext = File.Exists(path) ? Path.GetExtension(path) : "";
            CreateIcon(path);
            ImageKey = path;
            SubItems.Add("");
            SubItems.Add("");
            SubItems.Add("");
            SubItems[3].Tag = (long)0;

            watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path)
            };
            watcher.Deleted += new FileSystemEventHandler((object sender, FileSystemEventArgs e) =>
            {
                BucketListView lv = ListView as BucketListView;
                if (!lv.Updating)
                {
                    lv.StartUpdate();
                    ElapsedEventHandler evh = null;
                    evh = new ElapsedEventHandler((object s, ElapsedEventArgs ev) =>
                    {
                        Global.layoutLockTimer.Elapsed -= evh;
                        lv.StopUpdate();
                        Global.layoutLockTimer.Stop();
                    });
                    Global.layoutLockTimer.Elapsed += evh;
                }
                Remove();
                Global.layoutLockTimer.Stop();
                Global.layoutLockTimer.Start();
            });
            watcher.Changed += new FileSystemEventHandler((object sender, FileSystemEventArgs e) =>
            {
                updateDetail();
            });
            watcher.Renamed += new RenamedEventHandler((object sender, RenamedEventArgs e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    Name = e.FullPath;
                    RemoveIcon(e.OldFullPath);
                    CreateIcon(e.FullPath);
                    ImageKey = e.FullPath;
                    updateDetail();
                    watcher.Filter = Path.GetFileName(e.FullPath);
                }
            });
            watcher.EnableRaisingEvents = true;
            updateDetail();
        }

        private void CreateIcon(string path)
        {
            if (!Global.imgList.Images.ContainsKey(path))
            {
                if (Properties.Settings.Default.UseThumbnail && File.Exists(path) && Consts.imageExts.Contains(Path.GetExtension(path).ToLower()))
                {
                    Image img = Image.FromFile(path);
                    Global.imgList.Images.Add(path, img.GetThumbnailImage(128, 128, () => false, IntPtr.Zero));
                    img.Dispose();
                }
                else
                    Global.imgList.Images.Add(path, ShellInfoHelper.GetIconFromPath(path, Properties.Settings.Default.IconSize == 16));
            }
        }
        private void RemoveIcon(string path)
        {
            if (Global.imgList.Images.ContainsKey(path))
            {
                Global.imgList.Images.RemoveByKey(path);
            }
        }

        override public void Remove()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            if (ListView != null)
            {
                BucketTabPage tp = ListView.Parent as BucketTabPage;
                base.Remove();
                tp.SetName(tp.Name);
            }
        }

        internal virtual void updateDetail()
        {
            Text = ShellInfoHelper.GetDisplayNameFromPath(Name);
        }
    }

    class ListViewDirectoryItem : ListViewFileSystemItem
    {
        public ListViewDirectoryItem(string path) : base(path) { }
        internal override void updateDetail()
        {
            base.updateDetail();
            SubItems[2].Text = Directory.GetLastWriteTime(Name).ToString("yyyy/MM/dd hh:mm");
        }
    }

    class ListViewFileItem : ListViewFileSystemItem
    {
        public ListViewFileItem(string path) : base(path) { }
        internal override void updateDetail()
        {
            base.updateDetail();
            FileInfo fileInfo = new FileInfo(Name);
            SubItems[1].Text = fileInfo.Extension;
            SubItems[2].Text = fileInfo.LastWriteTime.ToString("yyyy/MM/dd hh:mm");
            SubItems[3].Text = FileSizeHelper.GetHumanReadableFileSize(fileInfo.Length);
            SubItems[3].Tag = (long)fileInfo.Length;
        }
    }

    public class BucketTabPage : TabPage
    {
        public BucketListView BucketListView;

        public transDropBucket Form { get => FindForm() as transDropBucket; }
        public ListView.ListViewItemCollection BucketItems { get => BucketListView.Items; }

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
                foreach (ListViewItem item in BucketItems)
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

            ToolStripMenuItem dragEffectMenu = (ToolStripMenuItem)ContextMenuStrip.Items.Add("拖放模式");
            dragEffectMenu.DropDown.DropShadowEnabled = false;
            foreach (var kv in Consts.dragEffectDict)
            {
                ToolStripItem item = dragEffectMenu.DropDown.Items.Add(kv.Value);
                item.Click += new EventHandler((object sender, EventArgs e) =>
                 {
                     foreach (ToolStripMenuItem i in dragEffectMenu.DropDown.Items)
                     {
                         i.Checked = false;
                     }
                     ((ToolStripMenuItem)sender).Checked = true;
                     Properties.Settings.Default.DragEffect = kv.Key;
                 });
                if (kv.Key == Properties.Settings.Default.DragEffect)
                {
                    ((ToolStripMenuItem)item).Checked = true;
                }
            }

            ToolStripItem navigateItem = ContextMenuStrip.Items.Add("跳转已打开的资源管理器");
            ((ToolStripMenuItem)navigateItem).Checked = Properties.Settings.Default.UseNavigate;
            navigateItem.Click += new EventHandler((object sender, EventArgs e) =>
            {
                Properties.Settings.Default.UseNavigate = !Properties.Settings.Default.UseNavigate;
                ((ToolStripMenuItem)sender).Checked = Properties.Settings.Default.UseNavigate;
            });

            ToolStripItem thumbnailItem = ContextMenuStrip.Items.Add("图片文件显示缩略图");
            ((ToolStripMenuItem)thumbnailItem).Checked = Properties.Settings.Default.UseThumbnail;
            thumbnailItem.Click += new EventHandler((object sender, EventArgs e) =>
            {
                Form.UseThumbnail = !Form.UseThumbnail;
                ((ToolStripMenuItem)sender).Checked = Form.UseThumbnail;
            });

            ToolStripMenuItem iconSizeMenu = (ToolStripMenuItem)ContextMenuStrip.Items.Add("图标/缩略图大小");
            iconSizeMenu.DropDown.DropShadowEnabled = false;
            foreach (int size in new int[] { 16, 32, 64, 128 })
            {
                ToolStripItem item = iconSizeMenu.DropDown.Items.Add(size + "x" + size);
                item.Click += new EventHandler((object sender, EventArgs e) =>
                {
                    foreach (ToolStripMenuItem i in iconSizeMenu.DropDown.Items)
                    {
                        i.Checked = false;
                    }
                    ((ToolStripMenuItem)sender).Checked = true;
                    Form.IconSize = size;
                });
                if (size == Properties.Settings.Default.IconSize)
                {
                    ((ToolStripMenuItem)item).Checked = true;
                }
            }
        }

        public void SetName(string name)
        {
            Name = name;
            Text = name == "" ? BucketItems.Count.ToString() : name + " (" + BucketItems.Count + ")";
        }
    }

    public class BucketTabControl : TabControlWithEditableTab
    {
        public transDropBucket Form { get => FindForm() as transDropBucket; }

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
                BucketTabPage newTp = new BucketTabPage();
                TabPages.Insert(TabPages.IndexOf(contextTab) + 1, newTp);
                SelectedTab = newTp;
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
            tpCtxMnu.Items.Add("重命名").Click += new EventHandler((object sender, EventArgs e) =>
            {
                Rectangle rect = GetTabRect(TabPages.IndexOf(contextTab));
                OneTimeTextBox tb = new OneTimeTextBox()
                {
                    Text = contextTab.Name,
                    Location = new Point(rect.X + 2, rect.Y),
                    Size = new Size(TextRenderer.MeasureText(contextTab.Text, Font).Width, rect.Height)
                };
                tb.DisposeAction = () => contextTab.SetName(tb.Text);
                Form.Controls.Add(tb);
                tb.BringToFront();
                tb.Focus();
            });

            MouseDown += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                BucketTabPage tp = PointedTab as BucketTabPage;
                if (e.Button == MouseButtons.Left && e.Clicks == 1)
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
                        foreach (ListViewItem item in draggedTab.BucketItems)
                            paths.Add(item.Name);
                        if (paths.Count > 0)
                        {
                            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                            DoDragDrop(fileData, Properties.Settings.Default.DragEffect);
                        }
                        else
                        {
                            DoDragDrop(draggedTab, DragDropEffects.Move);
                        }
                    }
                }
            });

            MouseClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                BucketTabPage tp = PointedTab as BucketTabPage;
                if (e.Button == MouseButtons.Right)
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
                BucketTabPage tp = PointedTab as BucketTabPage;
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
                BucketTabPage targetTab = PointedTab as BucketTabPage;
                if (targetTab == tpPlus)
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (paths.Length == 0) return;
                    targetTab.BucketListView.AddFileSystemItems(paths);
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
                    targetTab.BucketListView.AddFileSystemItems(paths);
                }
            });
        }

        public void CreatePages(List<(string, List<string>)> data)
        {
            foreach (var tab in data)
            {
                CreatePage(tab.Item2, tab.Item1);
            }
        }

        public void CreatePage(List<string> paths, string name = "")
        {
            IntPtr h = Handle;
            BucketTabPage tp = new BucketTabPage()
            {
                Name = name
            };
            tp.BucketListView.AddFileSystemItems(paths);
            TabPages.Insert(TabPages.Count - 1, tp);
            SelectedTab = tp;
        }
    }
}
