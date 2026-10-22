// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Zen
{
    public static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_APPWINDOW = 0x00040000;

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint PrivateExtractIcons(string szFileName, int nIconIndex, int cxIcon, int cyIcon, IntPtr[] phicon, IntPtr[] piconid, uint nIcons, uint flags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_LARGEICON = 0x000000000;
        public const uint SHGFI_SMALLICON = 0x000000001;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItemImageFactory ppv);

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10,
            SIIGBF_CROPTOSQUARE = 0x20,
            SIIGBF_WIDETHUMBNAILS = 0x40,
            SIIGBF_ICONBACKGROUND = 0x80,
            SIIGBF_SCALEUP = 0x100
        }

        // --- Windows 10/11 True Acrylic Blur Behind ---
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        public enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        public enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_ENABLE_HOSTBACKDROP = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public static void EnableBlurBehind(IntPtr hwnd, uint gradientColor = 0x99181820)
        {
            try
            {
                AccentPolicy policy = new AccentPolicy();
                policy.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                policy.AccentFlags = 2; // Draw borders & full acrylic blur
                policy.GradientColor = (int)gradientColor;

                int size = Marshal.SizeOf(policy);
                IntPtr pPolicy = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(policy, pPolicy, false);

                WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.Data = pPolicy;
                data.SizeOfData = size;

                int res = SetWindowCompositionAttribute(hwnd, ref data);
                if (res != 0 && policy.AccentState == AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND)
                {
                    // Fallback to standard blur if acrylic not supported by this build
                    policy.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    Marshal.StructureToPtr(policy, pPolicy, false);
                    SetWindowCompositionAttribute(hwnd, ref data);
                }

                Marshal.FreeHGlobal(pPolicy);
            }
            catch { }
        }

        public static void DisableBlurBehind(IntPtr hwnd)
        {
            try
            {
                AccentPolicy policy = new AccentPolicy();
                policy.AccentState = AccentState.ACCENT_DISABLED;
                int size = Marshal.SizeOf(policy);
                IntPtr pPolicy = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(policy, pPolicy, false);

                WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.Data = pPolicy;
                data.SizeOfData = size;

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(pPolicy);
            }
            catch { }
        }

        // --- Low-Level Mouse Hook for Desktop Double Click ---
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetDoubleClickTime();

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public const int WH_MOUSE_LL = 14;
        public const int WM_LBUTTONUP = 0x0202;

        // --- Windows Desktop Icons Control ---
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const uint LVM_GETSELECTEDCOUNT = 0x1032;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static IntPtr GetDesktopDefView()
        {
            try
            {
                IntPtr hProgman = FindWindow("Progman", "Program Manager");
                IntPtr hDefView = IntPtr.Zero;
                if (hProgman != IntPtr.Zero)
                {
                    hDefView = FindWindowEx(hProgman, IntPtr.Zero, "SHELLDLL_DefView", null);
                }

                if (hDefView == IntPtr.Zero)
                {
                    IntPtr hWorker = IntPtr.Zero;
                    while ((hWorker = FindWindowEx(IntPtr.Zero, hWorker, "WorkerW", null)) != IntPtr.Zero)
                    {
                        IntPtr hDef = FindWindowEx(hWorker, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (hDef != IntPtr.Zero)
                        {
                            hDefView = hDef;
                            break;
                        }
                    }
                }
                return hDefView;
            }
            catch { }
            return IntPtr.Zero;
        }

        public static IntPtr GetDesktopListView()
        {
            IntPtr hDefView = GetDesktopDefView();
            if (hDefView != IntPtr.Zero)
            {
                return FindWindowEx(hDefView, IntPtr.Zero, "SysListView32", null);
            }
            return IntPtr.Zero;
        }

        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const uint LWA_ALPHA = 0x02;

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private static readonly object _fadeLock = new object();
        private static bool _desktopIconsFadedOut = false;

        public static bool AreDesktopIconsVisible()
        {
            if (_desktopIconsFadedOut) return false;
            IntPtr hList = GetDesktopListView();
            if (hList != IntPtr.Zero)
            {
                int exStyle = GetWindowLong(hList, GWL_EXSTYLE);
                if ((exStyle & WS_EX_TRANSPARENT) != 0) return false;
                return IsWindowVisible(hList);
            }
            return true;
        }

        public static void SetDesktopIconsVisible(bool visible)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                lock (_fadeLock)
                {
                    try
                    {
                        IntPtr hDefView = GetDesktopDefView();
                        if (hDefView != IntPtr.Zero)
                        {
                            // Always ensure wallpaper remains visible
                            ShowWindow(hDefView, SW_SHOW);
                        }

                        IntPtr hList = GetDesktopListView();
                        if (hList == IntPtr.Zero) return;

                        if (visible)
                        {
                            // Ensure window is at (0, 0) and visible
                            SetWindowPos(hList, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                            ShowWindow(hList, SW_SHOW);

                            int exStyle = GetWindowLong(hList, GWL_EXSTYLE);
                            SetWindowLong(hList, GWL_EXSTYLE, (exStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT);

                            // Smooth in-place fade in (0 -> 255)
                            for (int a = 0; a <= 255; a += 35)
                            {
                                SetLayeredWindowAttributes(hList, 0, (byte)Math.Min(255, a), LWA_ALPHA);
                                Thread.Sleep(16);
                            }
                            SetLayeredWindowAttributes(hList, 0, 255, LWA_ALPHA);

                            // Remove layered style when fully opaque for standard native rendering
                            exStyle = GetWindowLong(hList, GWL_EXSTYLE);
                            SetWindowLong(hList, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);

                            _desktopIconsFadedOut = false;
                        }
                        else
                        {
                            // Ensure layered style is enabled
                            int exStyle = GetWindowLong(hList, GWL_EXSTYLE);
                            SetWindowLong(hList, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

                            // Smooth in-place fade out (255 -> 0)
                            for (int a = 255; a >= 0; a -= 35)
                            {
                                SetLayeredWindowAttributes(hList, 0, (byte)Math.Max(0, a), LWA_ALPHA);
                                Thread.Sleep(16);
                            }
                            SetLayeredWindowAttributes(hList, 0, 0, LWA_ALPHA);

                            // Set transparent so all mouse clicks pass right through to wallpaper
                            exStyle = GetWindowLong(hList, GWL_EXSTYLE);
                            SetWindowLong(hList, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);

                            _desktopIconsFadedOut = true;
                        }

                        try
                        {
                            Microsoft.Win32.Registry.SetValue(
                                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                                "HideIcons",
                                visible ? 0 : 1,
                                Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        catch { }
                    }
                    catch { }
                }
            });
        }

        public static bool ToggleDesktopIcons()
        {
            bool current = AreDesktopIconsVisible();
            bool target = !current;
            _desktopIconsFadedOut = !target;
            SetDesktopIconsVisible(target);
            return target;
        }

        public static int GetDesktopListViewSelectedCount()
        {
            try
            {
                IntPtr hList = GetDesktopListView();
                if (hList != IntPtr.Zero)
                {
                    return SendMessage(hList, LVM_GETSELECTEDCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();
                }
            }
            catch { }
            return 0;
        }

        public static void TrimWorkingSet()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                using (Process p = Process.GetCurrentProcess())
                {
                    EmptyWorkingSet(p.Handle);
                }
            }
            catch
            {
            }
        }
    }
}
