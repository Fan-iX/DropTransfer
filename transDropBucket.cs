﻿using System;
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
using System.Windows.Forms.VisualStyles;


namespace DropTransfer
{
    public class Global
    {
        public static ShellContextMenu ctxMnu = new ShellContextMenu();
        public static ImageList imgList = new ImageList();
    }

    public class transDropBucket : PocketForm
    {
        private DragDropEffects _dragEffect = DragDropEffects.All;
        public DragDropEffects DragEffect
        {
            get => _dragEffect;
            set
            {
                _dragEffect = value;
                if (value == DragDropEffects.All)
                    Text = "拖放中转站 [自动]";
                else if (value == DragDropEffects.Copy)
                    Text = "拖放中转站 [复制]";
                else if (value == DragDropEffects.Move)
                    Text = "拖放中转站 [移动]";
            }
        }
        private BucketTabControl tc = new BucketTabControl()
        {
            Dock = DockStyle.Fill,
            SizeMode = TabSizeMode.Normal,
            TabStop = false
        };

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            List<(string,List<string>)> history = tc.TabPages.Cast<BucketTabPage>().Select(
                x => new ValueTuple<string, List<string>>(x.Name, x.BucketListView.Items.Cast<ListViewItem>().Select(y => y.Name).ToList())
            ).Where(l => l.Item2.Count > 0).ToList();
            UnfoldedSize = new Size(this.ClientSize.Width, Math.Max(this.ClientSize.Height, 150 * DpiScale));
            Properties.Settings.Default.SelectedIndex = tc.SelectedIndex;
            Properties.Settings.Default.UnfoldedSize = this.UnfoldedSize;
            Properties.Settings.Default.WindowLocation = this.Location;
            Properties.Settings.Default.DragEffect = this.DragEffect;
            Properties.Settings.Default.History = history;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }

        private void ResizeControl()
        {
            Global.imgList.ImageSize = new Size(
                16 * DpiScale,
                16 * DpiScale
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
            ResizeControl();
        }

        public transDropBucket()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(transDropBucket));
            UnfoldedSize = Properties.Settings.Default.UnfoldedSize;
            DragEffect = Properties.Settings.Default.DragEffect;

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
            List<(string,List<string>)> history = Properties.Settings.Default.History;
            int index = Properties.Settings.Default.SelectedIndex;

            if (history.Count > 0)
            {
                tc.CreatePages(history);
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

            ResizeControl();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return base.ProcessCmdKey(ref msg, keyData);
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

            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Focus();
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                if (item != null)
                {
                    item.Focused = true;
                }
            });
            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                SelectedItems.Clear();
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            });
            ItemDrag += new ItemDragEventHandler((object sender, ItemDragEventArgs e) =>
            {
                List<string> paths = new List<string>();
                foreach (ListViewItem item in SelectedItems)
                    paths.Add(item.Name);
                DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                DoDragDrop(fileData, (FindForm() as transDropBucket).DragEffect);
            });
            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                if (item == null || point.Y > item.Bounds.Top + item.Bounds.Height / 2)
                    InsertItemsAfter(item, paths);
                else
                    InsertItemsBefore(item, paths);
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
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void AddItems(List<string> paths)
        {
            if (paths == null || paths.Count == 0) return;
            BeginUpdate();
            foreach (string path in paths)
                AddItem(path);
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void InsertItemsBefore(ListViewItem anchor, string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            BeginUpdate();
            SelectedItems.Clear();
            anchor = InsertItemBefore(anchor, paths[0]);
            foreach (string path in paths.Skip(1))
            {
                anchor = InsertItemAfter(anchor, path);
                anchor.Selected = true;
            }
            anchor.Focused = true;
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }
        public void InsertItemsAfter(ListViewItem anchor, string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            BeginUpdate();
            SelectedItems.Clear();
            foreach (string path in paths)
            {
                anchor = InsertItemAfter(anchor, path);
                anchor.Selected = true;
            }
            anchor.Focused = true;
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void AddItems(StringCollection paths)
        {
            if (paths == null || paths.Count == 0) return;
            BeginUpdate();
            foreach (string path in paths)
                AddItem(path);
            EndUpdate();
            Refresh();
            (Parent as BucketTabPage).SetName(Parent.Name);
        }

        public void AddItem(string sourceName)
        {
            if (Items.ContainsKey(sourceName))
            {
                ListViewItem item = Items[sourceName];
                Items.Remove(item);
                Items.Add(item);
            }
            else if (Directory.Exists(sourceName))
                Items.Add(new ListViewDirectoryItem(sourceName));
            else if (File.Exists(sourceName))
                Items.Add(new ListViewFileItem(sourceName));
        }

        public ListViewItem InsertItemBefore(ListViewItem anchor, string sourceName)
        {
            ListViewItem item = null;
            if (Items.ContainsKey(sourceName))
            {
                if (anchor != null && anchor.Name == sourceName) return anchor;
                item = Items[sourceName];
                Items.Remove(item);
                if (anchor == null || !Items.Contains(anchor))
                    item = Items.Insert(0, item);
                else
                    item = Items.Insert(Items.IndexOf(anchor), item);
            }
            else
            {
                item = Directory.Exists(sourceName) ? new ListViewDirectoryItem(sourceName) :
                File.Exists(sourceName) ? new ListViewFileItem(sourceName) : null;
                if (item == null) return null;
                if (anchor == null || !Items.Contains(anchor))
                    item = Items.Insert(0, item);
                else
                    item = Items.Insert(Items.IndexOf(anchor), item);
            }
            item.Selected = true;
            item.Focused = true;
            return item;
        }

        public ListViewItem InsertItemAfter(ListViewItem anchor, string sourceName)
        {
            ListViewItem item = null;
            if (Items.ContainsKey(sourceName))
            {
                if (anchor != null && anchor.Name == sourceName) return anchor;
                item = Items[sourceName];
                Items.Remove(item);
                if (anchor == null || !Items.Contains(anchor))
                    return Items.Add(item);
                else
                    return Items.Insert(Items.IndexOf(anchor) + 1, item);
            }
            else
            {
                item = Directory.Exists(sourceName) ? new ListViewDirectoryItem(sourceName) :
               File.Exists(sourceName) ? new ListViewFileItem(sourceName) : null;
                if (item == null) return null;
                if (anchor == null || !Items.Contains(anchor))
                    item = Items.Add(item);
                else
                    item = Items.Insert(Items.IndexOf(anchor) + 1, item);
            }
            item.Selected = true;
            item.Focused = true;
            return item;
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
                transDropBucket form = FindForm() as transDropBucket;
                if (form.DragEffect == DragDropEffects.All)
                    form.DragEffect = DragDropEffects.Copy;
                else if (form.DragEffect == DragDropEffects.Copy)
                    form.DragEffect = DragDropEffects.Move;
                else if (form.DragEffect == DragDropEffects.Move)
                    form.DragEffect = DragDropEffects.All;
            });
        }

        public void SetName(string name)
        {
            Name = name;
            Text = name == "" ? BucketListView.Items.Count.ToString() : name + " (" + BucketListView.Items.Count + ")";
        }
    }

    public class BucketTabControl : TabControlWithEditableTab
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
                FindForm().Controls.Add(tb);
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
                        foreach (ListViewItem item in draggedTab.BucketListView.Items)
                            paths.Add(item.Name);
                        if (paths.Count > 0)
                        {
                            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                            DoDragDrop(fileData, (FindForm() as transDropBucket).DragEffect);
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

        public void CreatePages(List<(string,List<string>)> data)
        {
            foreach (var tab in data)
            {
                CreatePage(tab.Item2, tab.Item1);
            }
        }

        public void CreatePage(List<string> paths, string name = "")
        {
            IntPtr h = Handle;
            BucketTabPage tp = new BucketTabPage(){
                Name = name
            };
            tp.BucketListView.AddItems(paths);
            TabPages.Insert(TabPages.Count - 1, tp);
            SelectedTab = tp;
        }
    }
}
