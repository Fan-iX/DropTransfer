using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
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
    public const uint SHGFI_SYSICONINDEX = 0x4000;

    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
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
    public static extern IntPtr SHGetFileInfo(string pszPath,
                                              uint dwFileAttributes,
                                              out SHFILEINFO psfi,
                                              uint cbSizeFileInfo,
                                              uint uFlags);

    public static Image GetIconFromPath(string pszPath)
    {
        SHFILEINFO info = new SHFILEINFO();
        SHGetFileInfo(pszPath, 0, out info, (uint)Marshal.SizeOf(info), SHGFI_ICON | SHGFI_SMALLICON);
        if (info.hIcon == (IntPtr)0)
            return new Bitmap(32, 32);
        else
            return Icon.FromHandle(info.hIcon).ToBitmap();
    }

    public static string GetDisplayNameFromPath(string pszPath)
    {
        SHFILEINFO info = new SHFILEINFO();
        SHGetFileInfo(pszPath, 0, out info, (uint)Marshal.SizeOf(info), SHGFI_DISPLAYNAME);
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

public class ShellApplication
{
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
