using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

public static class FileSizeHelper
{
    static readonly string[] SizeSuffixes = { "B ", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    const long byteConversion = 1000;
    public static string GetHumanReadableFileSize(long value)
    {
        if (value == 0) { return "0"; }

        int mag = (int)Math.Log(value, byteConversion);
        int adjustedSize = (int)(value / Math.Pow(1000, mag));

        return string.Format("{0:d} {1}", adjustedSize, SizeSuffixes[mag]);
    }
}

public class FileSystemHelper
{
    public static void OperateFileSystemItem(
        string sourceName,
        string destinationName,
        DragDropEffects operationEffect
    )
    {
        UIOption showUI = UIOption.AllDialogs;
        UICancelOption onUserCancel = UICancelOption.DoNothing;
        if (operationEffect.HasFlag(DragDropEffects.Move))
        {
            if (sourceName == destinationName) return;
            if (File.Exists(sourceName))
                FileSystem.MoveFile(sourceName, destinationName, showUI, onUserCancel);
            else if (Directory.Exists(sourceName))
                FileSystem.MoveDirectory(sourceName, destinationName, showUI, onUserCancel);
        }
        else
        {
            if (sourceName == destinationName) destinationName = Path.Combine(
                Path.GetDirectoryName(sourceName),
                Path.GetFileNameWithoutExtension(sourceName) + " - Copy" +
                Path.GetExtension(sourceName)
            );
            if (File.Exists(sourceName))
                FileSystem.CopyFile(sourceName, destinationName, showUI, onUserCancel);
            else if (Directory.Exists(sourceName))
                FileSystem.CopyDirectory(sourceName, destinationName, showUI, onUserCancel);
        }
    }

    public static void DeleteFileSystemItem(
        string itemName,
        RecycleOption recycle
    )
    {
        UIOption showUI = UIOption.AllDialogs;
        UICancelOption onUserCancel = UICancelOption.DoNothing;
        if (File.Exists(itemName))
            FileSystem.DeleteFile(itemName, showUI, recycle, onUserCancel);
        else if (Directory.Exists(itemName))
            FileSystem.DeleteDirectory(itemName, showUI, recycle, onUserCancel);
    }
}

public class ShellInfoHelper
{
    public const uint SHGFI_DISPLAYNAME = 0x200;
    public const uint SHGFI_ICON = 0x100;
    public const uint SHGFI_LARGEICON = 0x0;
    public const uint SHGFI_SMALLICON = 0x1;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    public const uint SHGFI_SYSICONINDEX = 0x4000;

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHFILEINFOW
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    public static string GetDownloadFolderPath()
    {
        return SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero);
    }
    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                                                                                          uint dwFlags,
                                                                                          IntPtr hToken);

    [DllImport("shell32")]
    public static extern IntPtr SHGetFileInfoW([MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                                              uint dwFileAttributes,
                                              out SHFILEINFOW psfi,
                                              uint cbSizeFileInfo,
                                              uint uFlags);
    [DllImport("user32")]
    public static extern int DestroyIcon(IntPtr hIcon);

    public static Image GetIconFromPath(string pszPath, bool simallIcon = true)
    {
        SHFILEINFOW info = new SHFILEINFOW();
        uint flag = SHGFI_ICON;
        flag |= simallIcon ? SHGFI_SMALLICON : SHGFI_LARGEICON;
        SHGetFileInfoW(pszPath, 0, out info, (uint)Marshal.SizeOf(info), flag);
        if (info.hIcon == (IntPtr)0)
        {
            DestroyIcon(info.hIcon);
            return new Bitmap(32, 32);
        }
        else
        {
            Icon icon = Icon.FromHandle(info.hIcon);
            Image img = icon.ToBitmap();
            icon.Dispose();
            DestroyIcon(info.hIcon);
            return img;
        }
    }

    public static string GetDisplayNameFromPath(string pszPath)
    {
        SHFILEINFOW info = new SHFILEINFOW();
        SHGetFileInfoW(pszPath, 0, out info, (uint)Marshal.SizeOf(info), SHGFI_DISPLAYNAME);
        return info.szDisplayName;
    }

    public static string GetExactPathName(string pathName)
    {
        var di = new DirectoryInfo(pathName);

        if (di.Parent != null)
        {
            return Path.Combine(
                GetExactPathName(di.Parent.FullName),
                di.Parent.GetFileSystemInfos(di.Name)[0].Name);
        }
        else
        {
            if (di.FullName.StartsWith("\\\\") || di.FullName.StartsWith("//"))
                return Path.GetPathRoot(di.FullName);
            else
                return di.Name.ToUpper();
        }
    }
}

public class ShellApplicationHelper
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    internal interface IServiceProvider
    {
        void QueryService([MarshalAs(UnmanagedType.LPStruct)] Guid guidService, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
    }
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214e2-0000-0000-c000-000000000046")]
    internal interface IShellBrowser
    {
        IntPtr GetWindow();
    }
    public static IntPtr GetTabHwnd(object window)
    {
        IServiceProvider sp = window as IServiceProvider;
        object sb;
        sp.QueryService(Guid.Parse("000214e2-0000-0000-c000-000000000046"), typeof(IShellBrowser).GUID, out sb);
        IShellBrowser shellBrowser = (IShellBrowser)sb;
        return (IntPtr)shellBrowser.GetWindow();
    }
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32")]
    private static extern void GetClassName(IntPtr hWnd, StringBuilder s, int nMaxCount);
    public static string GetClassName(IntPtr hWnd)
    {
        StringBuilder sb = new StringBuilder(256);
        GetClassName(hWnd, sb, 256);
        return sb.ToString();
    }
    [DllImport("user32")]
    public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);
    public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);
    private static bool EnumTabWindow(IntPtr handle, IntPtr pointer)
    {
        GCHandle gch = GCHandle.FromIntPtr(pointer);
        List<IntPtr> list = gch.Target as List<IntPtr>;
        if (GetClassName(handle) == "ShellTabWindowClass")
        {
            list.Add(handle);
        }
        return true;
    }
    public static IntPtr GetActiveTabHwnd(IntPtr parent)
    {
        List<IntPtr> result = new List<IntPtr>();
        GCHandle listHandle = GCHandle.Alloc(result);
        try
        {
            EnumWindowProc childProc = new EnumWindowProc(EnumTabWindow);
            EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
        }
        finally
        {
            if (listHandle.IsAllocated)
                listHandle.Free();
        }
        return result[0];
    }
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void NavigateExplorerTo(string path)
    {
        if (!Directory.Exists(path)) return;
        IntPtr TopExplorerHwnd = FindWindow("CabinetWClass", null);
        if (TopExplorerHwnd == IntPtr.Zero)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return;
        }
        IntPtr activateTabHwnd = GetActiveTabHwnd(TopExplorerHwnd);

        object shellApplication = System.Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
        object windows = shellApplication.GetType().InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shellApplication, null);
        int windowCounts = (int)windows.GetType().InvokeMember("Count", System.Reflection.BindingFlags.GetProperty, null, windows, null);

        for (int i = 0; i < windowCounts; i++)
        {
            object window = windows.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, windows, new object[] { i });
            IntPtr hwnd = (IntPtr)(long)window.GetType().InvokeMember("HWND", System.Reflection.BindingFlags.GetProperty, null, window, null);
            IntPtr tabHwnd = GetTabHwnd(window);
            if (hwnd != TopExplorerHwnd || tabHwnd != activateTabHwnd)
            {
                Marshal.ReleaseComObject(window);
                continue;
            }
            else
            {
                if (path == "")
                {
                    window.GetType().InvokeMember("Refresh", System.Reflection.BindingFlags.InvokeMethod, null, window, null);
                }
                else
                {
                    window.GetType().InvokeMember("Navigate", System.Reflection.BindingFlags.InvokeMethod, null, window, new object[] { new Uri(path).ToString() });
                }
                Marshal.ReleaseComObject(window);
                break;
            }
        }
        Marshal.ReleaseComObject(windows);
        Marshal.ReleaseComObject(shellApplication);
    }
    public static List<string> GetExplorerPaths()
    {
        object shellApplication = System.Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
        object windows = shellApplication.GetType().InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shellApplication, null);
        int windowCounts = (int)windows.GetType().InvokeMember("Count", System.Reflection.BindingFlags.GetProperty, null, windows, null);
        List<string> output = new List<string>();
        for (int i = 0; i < windowCounts; i++)
        {
            object window = windows.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, windows, new object[] { i });
            string locationURL = (string)window.GetType().InvokeMember("LocationURL", System.Reflection.BindingFlags.GetProperty, null, window, null);
            if (locationURL != "")
            {
                output.Add(new Uri(locationURL).LocalPath);
            }
            Marshal.ReleaseComObject(window);
        };
        Marshal.ReleaseComObject(windows);
        Marshal.ReleaseComObject(shellApplication);
        return output;
    }
}


namespace DropTransfer
{
    public class DpiFactor
    {
        private float scaling;
        public DpiFactor(float scale)
        {
            scaling = scale;
        }
        public static int operator *(int pt, DpiFactor factor)
        {
            return (int)(pt * factor.scaling);
        }
    }
}
