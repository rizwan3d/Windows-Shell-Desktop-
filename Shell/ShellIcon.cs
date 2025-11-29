using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shell;

public static class ShellIcon
{
    [Flags]
    private enum SHGFI : uint
    {
        Icon = 0x000000100,
        LargeIcon = 0x000000000,   // 32x32
        SmallIcon = 0x000000001,   // 16x16
        UseFileAttributes = 0x000000010,
        PIDL = 0x000000008
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        IntPtr pszPath,        // can be PIDL if SHGFI.PIDL is set
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        SHGFI uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        SHGFI uFlags);

    [DllImport("shell32.dll")]
    private static extern int SHGetSpecialFolderLocation(
        IntPtr hwndOwner,
        int nFolder,
        out IntPtr ppidl);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr pv);

    public const int CSIDL_DRIVES = 0x0011; // "This PC"
    public const int CSIDL_NETWORK = 0x0012; // "Network"
    public const int CSIDL_BITBUCKET = 0x000a; // "Recycle Bin"
    public const int CSIDL_CONTROLS = 0x0003; // "Control Panel"

    public static ImageSource? GetFileIconSource(string path, bool small = false)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        SHFILEINFO shinfo;
        SHGFI flags = SHGFI.Icon |
                      SHGFI.UseFileAttributes |
                      (small ? SHGFI.SmallIcon : SHGFI.LargeIcon);

        IntPtr res = SHGetFileInfo(
            path,
            0,
            out shinfo,
            (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
            flags);

        if (res == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
            return null;

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(shinfo.hIcon);
        }
    }

    public static ImageSource? GetSpecialFolderIcon(int csidl, bool small = false)
    {
        IntPtr pidl;
        int hr = SHGetSpecialFolderLocation(IntPtr.Zero, csidl, out pidl);
        if (hr != 0 || pidl == IntPtr.Zero)
            return null;

        try
        {
            SHFILEINFO shinfo;
            SHGFI flags = SHGFI.Icon |
                          SHGFI.PIDL |
                          (small ? SHGFI.SmallIcon : SHGFI.LargeIcon);

            IntPtr res = SHGetFileInfo(
                pidl,
                0,
                out shinfo,
                (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                flags);

            if (res == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                return null;

            try
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                DestroyIcon(shinfo.hIcon);
            }
        }
        finally
        {
            CoTaskMemFree(pidl);
        }
    }
}
