// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Zen
{
    public static class IconHelper
    {
        private static readonly Dictionary<string, ImageSource> _cache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        public static ImageSource GetIconForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return GetDefaultAppIcon();
            }

            try
            {
                string cacheKey = path;
                bool isDir = Directory.Exists(path);

                // Never cache shortcuts, executables, or specific custom icons by extension
                if (!isDir && !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                           && !path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
                           && !path.EndsWith(".url", StringComparison.OrdinalIgnoreCase)
                           && !path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
                           && !path.EndsWith(".appref-ms", StringComparison.OrdinalIgnoreCase))
                {
                    string ext = Path.GetExtension(path);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        cacheKey = ext;
                    }
                }

                if (_cache.ContainsKey(cacheKey))
                {
                    return _cache[cacheKey];
                }

                // 1. If .url file (e.g., Steam or Epic shortcut), parse IconFile and IconIndex
                if (path.EndsWith(".url", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                {
                    try
                    {
                        string iconFile = null;
                        int iconIndex = 0;
                        string[] lines = File.ReadAllLines(path);
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                            {
                                iconFile = line.Substring(9).Trim();
                            }
                            else if (line.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                            {
                                int idx;
                                if (int.TryParse(line.Substring(10).Trim(), out idx))
                                {
                                    iconIndex = idx;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(iconFile))
                        {
                            iconFile = Environment.ExpandEnvironmentVariables(iconFile);
                            if (File.Exists(iconFile))
                            {
                                BitmapSource bsFromUrl = ExtractIconFromFile(iconFile, iconIndex, 48);
                                if (bsFromUrl != null)
                                {
                                    _cache[cacheKey] = bsFromUrl;
                                    return bsFromUrl;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                // 2. High-resolution Shell extraction via IShellItemImageFactory (48x48)
                NativeMethods.IShellItemImageFactory factory = null;
                try
                {
                    Guid guid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                    NativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, guid, out factory);
                    if (factory != null)
                    {
                        IntPtr hBitmap;
                        int hr = factory.GetImage(
                            new NativeMethods.SIZE(48, 48),
                            NativeMethods.SIIGBF.SIIGBF_BIGGERSIZEOK | NativeMethods.SIIGBF.SIIGBF_ICONONLY,
                            out hBitmap);

                        if (hr == 0 && hBitmap != IntPtr.Zero)
                        {
                            try
                            {
                                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                bs.Freeze();
                                _cache[cacheKey] = bs;
                                return bs;
                            }
                            finally
                            {
                                NativeMethods.DeleteObject(hBitmap);
                            }
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    if (factory != null)
                    {
                        try
                        {
                            Marshal.ReleaseComObject(factory);
                        }
                        catch
                        {
                        }
                    }
                }

                // 3. For .lnk, .exe, or .ico, extract directly via PrivateExtractIcons
                if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    BitmapSource bsDirect = ExtractIconFromFile(path, 0, 48);
                    if (bsDirect != null)
                    {
                        _cache[cacheKey] = bsDirect;
                        return bsDirect;
                    }
                }

                // 4. Fallback: standard SHGetFileInfo (32x32 large icon)
                NativeMethods.SHFILEINFO shinfo = new NativeMethods.SHFILEINFO();
                uint flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON;

                if (!File.Exists(path) && !isDir)
                {
                    flags |= NativeMethods.SHGFI_USEFILEATTRIBUTES;
                }

                uint attributes = isDir ? 0x00000010u /*FILE_ATTRIBUTE_DIRECTORY*/ : 0x00000080u /*FILE_ATTRIBUTE_NORMAL*/;

                IntPtr result = NativeMethods.SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        _cache[cacheKey] = bs;
                        return bs;
                    }
                    finally
                    {
                        NativeMethods.DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return GetDefaultAppIcon();
        }

        private static BitmapSource ExtractIconFromFile(string file, int iconIndex, int size)
        {
            try
            {
                IntPtr[] phicon = new IntPtr[1];
                IntPtr[] piconid = new IntPtr[1];
                uint n = NativeMethods.PrivateExtractIcons(file, iconIndex, size, size, phicon, piconid, 1, 0);
                if (n > 0 && phicon[0] != IntPtr.Zero)
                {
                    try
                    {
                        BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                            phicon[0],
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        return bs;
                    }
                    finally
                    {
                        NativeMethods.DestroyIcon(phicon[0]);
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public static ImageSource GetDefaultAppIcon()
        {
            const string defaultKey = "__default_app__";
            if (_cache.ContainsKey(defaultKey))
            {
                return _cache[defaultKey];
            }

            try
            {
                string explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                NativeMethods.SHFILEINFO shinfo = new NativeMethods.SHFILEINFO();
                NativeMethods.SHGetFileInfo(explorerPath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);
                if (shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        _cache[defaultKey] = bs;
                        return bs;
                    }
                    finally
                    {
                        NativeMethods.DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
