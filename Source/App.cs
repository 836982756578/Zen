// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace Zen
{
    public class App : System.Windows.Application
    {
        private static App _instance;
        private static Mutex _mutex;
        private NotifyIcon _trayIcon;
        private AppConfig _config;
        private readonly List<ZenWindow> _zenWindows = new List<ZenWindow>();
        private bool _allZenHidden = false;

        // Desktop Double-Click Mouse Hook
        private static NativeMethods.LowLevelMouseProc _mouseProc;
        private static IntPtr _mouseHook = IntPtr.Zero;
        private static uint _lastDesktopClickTime = 0;
        private static NativeMethods.POINT _lastDesktopClickPoint;

        // Desktop Auto-Organize Watcher
        private FileSystemWatcher _desktopWatcher;

        [STAThread]
        public static void Main()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ev.ExceptionObject.ToString());
                    }
                    catch { }
                };

                bool createdNew;
                _mutex = new Mutex(true, "ZenFence_DesktopOrganizer_SingleInstance_9921", out createdNew);

                if (!createdNew)
                {
                    System.Windows.MessageBox.Show(
                        "Zen Fence - Masaüstü Düzenleyici zaten arka planda çalışıyor!\nEkranın sağ altındaki bildirim alanından (saat yanı ☯️ simgesi) kontrol edebilirsiniz.\n\nGeliştirici: Serkan ŞENTÜRK",
                        "Zen Fence - Masaüstü Düzenleyici",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                App app = new App();
                _instance = app;
                app.DispatcherUnhandledException += (s, ev) =>
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ev.Exception.ToString());
                        ev.Handled = true;
                    }
                    catch { }
                };
                app.Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ex.ToString());
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Load configuration
            _config = ConfigManager.LoadConfig();

            // Setup tray icon
            SetupTrayIcon();

            // Start automatic background update checker
            UpdateChecker.Start(_trayIcon);

            // Setup mouse hook for desktop double-click
            SetupMouseHook(_config.DesktopDoubleClickToggle);

            // Setup auto-organize if enabled
            SetupDesktopAutoOrganize(_config.AutoOrganizeEnabled);

            // Open Zen windows
            if (_config.ZenGroups == null || _config.ZenGroups.Count == 0)
            {
                _config = ConfigManager.CreateDefaultConfig();
            }

            foreach (ZenGroupData groupData in _config.ZenGroups)
            {
                CreateAndShowZenWindow(groupData);
            }
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Zen Fence - Masaüstü Düzenleyici | Serkan ŞENTÜRK";
            _trayIcon.Icon = GenerateAppIcon();
            _trayIcon.Visible = true;

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // Header title
            ToolStripMenuItem titleItem = new ToolStripMenuItem("☯️ Zen Fence - Masaüstü Düzenleyici");
            titleItem.Font = new Font(titleItem.Font, System.Drawing.FontStyle.Bold);
            titleItem.Enabled = false;
            contextMenu.Items.Add(titleItem);

            ToolStripMenuItem authorItem = new ToolStripMenuItem("👤 Geliştirici: Serkan ŞENTÜRK");
            authorItem.Font = new Font(authorItem.Font.FontFamily, 8.5f, System.Drawing.FontStyle.Italic);
            authorItem.Enabled = false;
            contextMenu.Items.Add(authorItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Add new Zen group
            ToolStripMenuItem addGroupItem = new ToolStripMenuItem("➕ Yeni Zen Grubu Ekle");
            addGroupItem.Click += (s, e) => AddNewGroup();
            contextMenu.Items.Add(addGroupItem);

            // Toggle visibility
            ToolStripMenuItem toggleVisibilityItem = new ToolStripMenuItem("👁️ Tüm Zen Gruplarını Gizle / Göster");
            toggleVisibilityItem.Click += (s, e) => ToggleAllGroups();
            contextMenu.Items.Add(toggleVisibilityItem);

            // Toggle desktop icons visibility
            ToolStripMenuItem toggleDesktopIconsItem = new ToolStripMenuItem("🖥️ Masaüstü Simgelerini Gizle / Göster");
            toggleDesktopIconsItem.Click += (s, e) => NativeMethods.ToggleDesktopIcons();
            contextMenu.Items.Add(toggleDesktopIconsItem);

            // Refresh Zen groups
            ToolStripMenuItem refreshItem = new ToolStripMenuItem("🔄 Zen Gruplarını Yenile");
            refreshItem.Click += (s, e) => RefreshAllGroups();
            contextMenu.Items.Add(refreshItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Desktop Double-Click Toggle
            ToolStripMenuItem doubleClickItem = new ToolStripMenuItem("🖱️ Masaüstüne Çift Tıklayınca Masaüstü Simgelerini Gizle/Göster");
            doubleClickItem.CheckOnClick = true;
            doubleClickItem.Checked = _config.DesktopDoubleClickToggle;
            doubleClickItem.ToolTipText = "Masaüstünde boş bir alana (duvar kağıdına) çift tıklandığında masaüstü simgelerini anında gizler veya gösterir.";
            doubleClickItem.Click += (s, e) =>
            {
                _config.DesktopDoubleClickToggle = doubleClickItem.Checked;
                SetupMouseHook(_config.DesktopDoubleClickToggle);
                ConfigManager.SaveConfig(_config);
            };
            contextMenu.Items.Add(doubleClickItem);

            // Desktop Double-Click Action Submenu
            ToolStripMenuItem doubleClickActionMenu = new ToolStripMenuItem("⚙️ Çift Tıklama Eylemi");
            ToolStripMenuItem actIcons = new ToolStripMenuItem("Masaüstü Simgelerini Gizle / Göster (Varsayılan)");
            ToolStripMenuItem actZen = new ToolStripMenuItem("Tüm Zen Gruplarını Gizle / Göster");
            ToolStripMenuItem actBoth = new ToolStripMenuItem("Hem Masaüstü Simgelerini Hem Zen Gruplarını Gizle / Göster");

            Action updateActionChecks = () =>
            {
                string cur = _config.DesktopDoubleClickAction ?? "ToggleDesktopIcons";
                actIcons.Checked = (cur == "ToggleDesktopIcons");
                actZen.Checked = (cur == "ToggleZenGroups");
                actBoth.Checked = (cur == "ToggleBoth");
            };
            updateActionChecks();

            actIcons.Click += (s, e) =>
            {
                _config.DesktopDoubleClickAction = "ToggleDesktopIcons";
                updateActionChecks();
                ConfigManager.SaveConfig(_config);
            };
            actZen.Click += (s, e) =>
            {
                _config.DesktopDoubleClickAction = "ToggleZenGroups";
                updateActionChecks();
                ConfigManager.SaveConfig(_config);
            };
            actBoth.Click += (s, e) =>
            {
                _config.DesktopDoubleClickAction = "ToggleBoth";
                updateActionChecks();
                ConfigManager.SaveConfig(_config);
            };

            doubleClickActionMenu.DropDownItems.Add(actIcons);
            doubleClickActionMenu.DropDownItems.Add(actZen);
            doubleClickActionMenu.DropDownItems.Add(actBoth);
            contextMenu.Items.Add(doubleClickActionMenu);

            // Auto-Organize Toggle
            ToolStripMenuItem autoOrganizeItem = new ToolStripMenuItem("⚡ Masaüstünü Otomatik Sınıflandır (Auto-Organize)");
            autoOrganizeItem.CheckOnClick = true;
            autoOrganizeItem.Checked = _config.AutoOrganizeEnabled;
            autoOrganizeItem.ToolTipText = "Masaüstüne gelen yeni görsel, belge ve programları otomatik gruplarına yerleştirir.";
            autoOrganizeItem.Click += (s, e) =>
            {
                _config.AutoOrganizeEnabled = autoOrganizeItem.Checked;
                SetupDesktopAutoOrganize(_config.AutoOrganizeEnabled);
                ConfigManager.SaveConfig(_config);
            };
            contextMenu.Items.Add(autoOrganizeItem);

            // Backup & Restore Menu
            ToolStripMenuItem backupMenu = new ToolStripMenuItem("💾 Masaüstü Düzeni Yedekleme");

            ToolStripMenuItem takeSnapshotItem = new ToolStripMenuItem("💾 Şimdi Düzen Yedeği Al (Snapshot)");
            takeSnapshotItem.Click += (s, e) =>
            {
                string path = ConfigManager.CreateBackup(_config);
                if (path != null)
                {
                    System.Windows.MessageBox.Show(
                        "Masaüstü düzen yedeğiniz başarıyla kaydedildi:\n\n" + Path.GetFileName(path),
                        "Zen - Düzen Yedeği",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            };
            backupMenu.DropDownItems.Add(takeSnapshotItem);

            ToolStripMenuItem restoreSnapshotItem = new ToolStripMenuItem("⏮️ Son Yedeği Geri Yükle");
            restoreSnapshotItem.Click += (s, e) =>
            {
                List<string> backups = ConfigManager.GetBackups();
                if (backups.Count == 0)
                {
                    System.Windows.MessageBox.Show("Henüz kayıtlı bir düzen yedeği bulunamadı.", "Zen - Yedekleme", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult res = System.Windows.MessageBox.Show(
                    "Son yedek (" + Path.GetFileName(backups[0]) + ") geri yüklensin mi?\nMevcut pencereler yenilenecektir.",
                    "Zen - Düzen Geri Yükle",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    AppConfig restored = ConfigManager.RestoreBackup(backups[0]);
                    if (restored != null)
                    {
                        _config = restored;
                        ReloadAllWindows();
                    }
                }
            };
            backupMenu.DropDownItems.Add(restoreSnapshotItem);

            ToolStripMenuItem openBackupsFolder = new ToolStripMenuItem("📂 Yedekler Klasörünü Aç");
            openBackupsFolder.Click += (s, e) =>
            {
                try { Process.Start("explorer.exe", ConfigManager.BackupDirectory); } catch { }
            };
            backupMenu.DropDownItems.Add(openBackupsFolder);

            contextMenu.Items.Add(backupMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Startup shortcut toggle
            ToolStripMenuItem startupItem = new ToolStripMenuItem("🚀 Windows Açılışında Başlat");
            startupItem.CheckOnClick = true;
            startupItem.Checked = ConfigManager.IsStartupEnabled();
            startupItem.Click += (s, e) =>
            {
                ConfigManager.SetStartupShortcut(startupItem.Checked);
                _config.RunOnStartup = startupItem.Checked;
                ConfigManager.SaveConfig(_config);
            };
            contextMenu.Items.Add(startupItem);

            // Single click to open toggle
            ToolStripMenuItem singleClickItem = new ToolStripMenuItem("🖱️ Tek Tıklamayla Aç");
            singleClickItem.CheckOnClick = true;
            singleClickItem.Checked = _config.SingleClickToOpen;
            singleClickItem.ToolTipText = "İşaretlenirse dosyalar tek tıklamayla, işaretli değilse standart çift tıklamayla açılır.";
            singleClickItem.Click += (s, e) =>
            {
                _config.SingleClickToOpen = singleClickItem.Checked;
                foreach (ZenWindow w in _zenWindows)
                {
                    w.SingleClickToOpen = _config.SingleClickToOpen;
                }
                ConfigManager.SaveConfig(_config);
            };
            contextMenu.Items.Add(singleClickItem);

            // Open config file
            ToolStripMenuItem openConfigItem = new ToolStripMenuItem("📁 Ayar Dosyasını Aç (JSON)");
            openConfigItem.Click += (s, e) =>
            {
                string configPath = ConfigManager.ConfigPath;
                if (File.Exists(configPath))
                {
                    Process.Start("notepad.exe", configPath);
                }
            };
            contextMenu.Items.Add(openConfigItem);

            // Open folder
            ToolStripMenuItem openFolderItem = new ToolStripMenuItem("📂 Uygulama Klasörünü Aç");
            openFolderItem.Click += (s, e) =>
            {
                Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);
            };
            contextMenu.Items.Add(openFolderItem);

            // Check for updates
            ToolStripMenuItem checkUpdateItem = new ToolStripMenuItem("🔄 Güncellemeleri Denetle");
            checkUpdateItem.Click += (s, e) => UpdateChecker.CheckForUpdateAsync(_trayIcon, isManual: true);
            contextMenu.Items.Add(checkUpdateItem);

            // GitHub developer profile
            ToolStripMenuItem devProfileItem = new ToolStripMenuItem("🌐 Geliştirici Sayfası (GitHub)");
            devProfileItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(AboutDialog.DeveloperGitHubUrl) { UseShellExecute = true }); } catch { }
            };
            contextMenu.Items.Add(devProfileItem);

            // GitHub project repository
            ToolStripMenuItem projectRepoItem = new ToolStripMenuItem("⭐ Zen Fence GitHub");
            projectRepoItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(AboutDialog.ProjectGitHubUrl) { UseShellExecute = true }); } catch { }
            };
            contextMenu.Items.Add(projectRepoItem);

            // About
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("ℹ️ Zen Fence Hakkında");
            aboutItem.Click += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AboutDialog dialog = new AboutDialog();
                    dialog.ShowDialog();
                }));
            };
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            ToolStripMenuItem exitItem = new ToolStripMenuItem("🚪 Çıkış");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.DoubleClick += (s, e) =>
            {
                ToggleAllGroups();
            };
        }

        private void SetupMouseHook(bool enable)
        {
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            if (!enable) return;

            try
            {
                _mouseProc = MouseHookCallback;
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    IntPtr hMod = NativeMethods.GetModuleHandle(curModule.ModuleName);
                    _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, hMod, 0);
                }
            }
            catch { }
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (int)wParam == NativeMethods.WM_LBUTTONUP)
            {
                try
                {
                    NativeMethods.MSLLHOOKSTRUCT hookStruct = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));
                    IntPtr hWnd = NativeMethods.WindowFromPoint(hookStruct.pt);
                    if (hWnd != IntPtr.Zero)
                    {
                        StringBuilder sb = new StringBuilder(256);
                        NativeMethods.GetClassName(hWnd, sb, 256);
                        string className = sb.ToString();

                        if (className == "Progman" || className == "WorkerW" || className == "SHELLDLL_DefView" || className == "SysListView32")
                        {
                            // If clicked on SysListView32, verify whether any desktop icon is selected.
                            // If an icon is selected, user clicked/opened that icon, not the empty desktop wallpaper.
                            if (className == "SysListView32")
                            {
                                if (NativeMethods.AreDesktopIconsVisible())
                                {
                                    int selCount = NativeMethods.GetDesktopListViewSelectedCount();
                                    if (selCount > 0)
                                    {
                                        _lastDesktopClickTime = 0;
                                        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                                    }
                                }
                            }

                            uint now = (uint)Environment.TickCount;
                            uint dcTime = NativeMethods.GetDoubleClickTime();
                            if (now - _lastDesktopClickTime <= dcTime &&
                                Math.Abs(hookStruct.pt.x - _lastDesktopClickPoint.x) <= 8 &&
                                Math.Abs(hookStruct.pt.y - _lastDesktopClickPoint.y) <= 8)
                            {
                                _lastDesktopClickTime = 0;
                                if (_instance != null && _instance._config.DesktopDoubleClickToggle)
                                {
                                    _instance.Dispatcher.BeginInvoke((Action)(() => _instance.OnDesktopDoubleClicked()));
                                }
                            }
                            else
                            {
                                _lastDesktopClickTime = now;
                                _lastDesktopClickPoint = hookStruct.pt;
                            }
                        }
                    }
                }
                catch { }
            }
            return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void SetupDesktopAutoOrganize(bool enable)
        {
            if (_desktopWatcher != null)
            {
                _desktopWatcher.EnableRaisingEvents = false;
                _desktopWatcher.Dispose();
                _desktopWatcher = null;
            }

            if (!enable) return;

            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (Directory.Exists(desktop))
                {
                    _desktopWatcher = new FileSystemWatcher(desktop);
                    _desktopWatcher.NotifyFilter = NotifyFilters.FileName;
                    _desktopWatcher.Created += (s, e) =>
                    {
                        ThreadPool.QueueUserWorkItem((state) =>
                        {
                            Thread.Sleep(900);
                            Dispatcher.BeginInvoke((Action)(() => AutoOrganizeFile(e.FullPath)));
                        });
                    };
                    _desktopWatcher.EnableRaisingEvents = true;
                }
            }
            catch { }
        }

        private void AutoOrganizeFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                string targetGroupName = null;

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp" || ext == ".gif" || ext == ".bmp")
                {
                    targetGroupName = "Görseller";
                }
                else if (ext == ".pdf" || ext == ".docx" || ext == ".xlsx" || ext == ".pptx" || ext == ".txt")
                {
                    targetGroupName = "Belgeler";
                }
                else if (ext == ".exe" || ext == ".msi")
                {
                    targetGroupName = "Yazılımlar";
                }

                if (targetGroupName != null)
                {
                    ZenWindow targetWindow = _zenWindows.Find(w => string.Equals(w.Data.Title, targetGroupName, StringComparison.CurrentCultureIgnoreCase));
                    if (targetWindow == null)
                    {
                        ZenGroupData newGroup = new ZenGroupData
                        {
                            Title = targetGroupName,
                            CustomEmoji = (targetGroupName == "Görseller" ? "📸" : (targetGroupName == "Belgeler" ? "💼" : "🚀")),
                            Left = 250,
                            Top = 200,
                            Width = 320,
                            Height = 250,
                            Items = new List<ZenItem>()
                        };
                        _config.ZenGroups.Add(newGroup);
                        ConfigManager.SaveConfig(_config);
                        CreateAndShowZenWindow(newGroup);
                        targetWindow = _zenWindows.Find(w => w.Data == newGroup);
                    }

                    if (targetWindow != null)
                    {
                        targetWindow.AddPathToGroup(filePath);
                        targetWindow.RefreshItems();
                        ConfigManager.SaveConfig(_config);
                    }
                }
            }
            catch { }
        }

        private void ReloadAllWindows()
        {
            foreach (ZenWindow win in _zenWindows.ToArray())
            {
                try { win.Close(); } catch { }
            }
            _zenWindows.Clear();

            foreach (ZenGroupData groupData in _config.ZenGroups)
            {
                CreateAndShowZenWindow(groupData);
            }
        }

        private Icon GenerateAppIcon()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(32, 32))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Background box
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(24, 24, 32)))
                    using (Pen borderPen = new Pen(Color.FromArgb(0, 122, 204), 2))
                    {
                        g.FillRectangle(bgBrush, 2, 2, 27, 27);
                        g.DrawRectangle(borderPen, 2, 2, 27, 27);
                    }

                    // Header bar
                    using (SolidBrush headBrush = new SolidBrush(Color.FromArgb(0, 122, 204)))
                    {
                        g.FillRectangle(headBrush, 3, 3, 26, 7);
                    }

                    // Small icon grid
                    using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                    {
                        g.FillRectangle(dotBrush, 6, 14, 6, 5);
                        g.FillRectangle(dotBrush, 15, 14, 6, 5);
                        g.FillRectangle(dotBrush, 6, 21, 6, 5);
                        g.FillRectangle(dotBrush, 15, 21, 6, 5);
                    }

                    IntPtr hIcon = bmp.GetHicon();
                    try
                    {
                        using (Icon temp = Icon.FromHandle(hIcon))
                        {
                            return (Icon)temp.Clone();
                        }
                    }
                    finally
                    {
                        NativeMethods.DestroyIcon(hIcon);
                    }
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void CreateAndShowZenWindow(ZenGroupData groupData)
        {
            ZenWindow win = new ZenWindow(groupData);
            win.SingleClickToOpen = _config.SingleClickToOpen;

            win.OnSingleClickOptionChanged += (val) =>
            {
                _config.SingleClickToOpen = val;
                foreach (ZenWindow w in _zenWindows)
                {
                    w.SingleClickToOpen = val;
                }
                ConfigManager.SaveConfig(_config);
            };

            win.OnConfigChanged += (w) =>
            {
                ConfigManager.SaveConfig(_config);
            };

            win.OnDeleteRequested += (w) =>
            {
                _zenWindows.Remove(w);
                _config.ZenGroups.Remove(w.Data);
                ConfigManager.SaveConfig(_config);
                w.Close();
            };

            win.OnAddNewGroupRequested += () =>
            {
                AddNewGroup();
            };

            _zenWindows.Add(win);
            win.Show();
        }

        private void AddNewGroup()
        {
            string title = InputDialog.Show(null, "Yeni Zen Grubu Oluştur", "Yeni Zen grubu için bir başlık belirleyin:", "Yeni Zen Grubu");
            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            ZenGroupData newGroup = new ZenGroupData
            {
                Title = title,
                Left = 200 + (_zenWindows.Count * 30),
                Top = 150 + (_zenWindows.Count * 30),
                Width = 320,
                Height = 250,
                ExpandedHeight = 250,
                BackgroundColor = "#1E1E24",
                Opacity = 0.55,
                Items = new List<ZenItem>()
            };

            _config.ZenGroups.Add(newGroup);
            ConfigManager.SaveConfig(_config);

            CreateAndShowZenWindow(newGroup);
        }

        private static uint _lastToggleTick = 0;

        public void OnDesktopDoubleClicked()
        {
            uint now = (uint)Environment.TickCount;
            if (now - _lastToggleTick < 400)
            {
                return;
            }
            _lastToggleTick = now;

            string action = _config.DesktopDoubleClickAction ?? "ToggleDesktopIcons";

            if (action == "ToggleZenGroups")
            {
                ToggleAllGroups();
            }
            else if (action == "ToggleBoth")
            {
                NativeMethods.ToggleDesktopIcons();
                ToggleAllGroups();
            }
            else // "ToggleDesktopIcons" (Default)
            {
                NativeMethods.ToggleDesktopIcons();
            }
        }

        public void ToggleAllGroups()
        {
            _allZenHidden = !_allZenHidden;

            foreach (ZenWindow win in _zenWindows)
            {
                if (_allZenHidden)
                {
                    win.Hide();
                }
                else
                {
                    win.Show();
                    win.ApplyDesktopMode();
                }
            }
        }

        private void RefreshAllGroups()
        {
            foreach (ZenWindow win in _zenWindows)
            {
                win.RefreshItems();
                win.ApplyVisualStyles();
                win.ApplyDesktopMode();
            }
        }

        private void ExitApplication()
        {
            ConfigManager.SaveConfig(_config);

            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            if (_desktopWatcher != null)
            {
                _desktopWatcher.EnableRaisingEvents = false;
                _desktopWatcher.Dispose();
                _desktopWatcher = null;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            foreach (ZenWindow win in _zenWindows.ToArray())
            {
                try
                {
                    win.Close();
                }
                catch
                {
                }
            }

            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }

            Shutdown();
        }
    }
}
