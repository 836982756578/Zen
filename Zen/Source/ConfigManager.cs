// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Zen
{
    public static class ConfigManager
    {
        public static string ConfigPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zen_config.json");
            }
        }

        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig config = serializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        if (config.ZenGroups == null)
                        {
                            config.ZenGroups = new List<ZenGroupData>();
                        }

                        if (string.IsNullOrEmpty(config.DesktopDoubleClickAction))
                        {
                            config.DesktopDoubleClickAction = "ToggleDesktopIcons";
                        }

                        if (config.ZenGroups != null && config.ZenGroups.Count > 0)
                        {
                            config.AppName = "Zen - Masaüstü Düzenleyici";
                            config.Creator = "Serkan ŞENTÜRK";
                            config.Author = "Serkan ŞENTÜRK";
                            config.Copyright = "© 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.";
                            SaveConfig(config);
                            return config;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Config load error: " + ex.Message);
            }

            return CreateDefaultConfig();
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                if (config == null) return;
                config.AppName = "Zen - Masaüstü Düzenleyici";
                config.Creator = "Serkan ŞENTÜRK";
                config.Author = "Serkan ŞENTÜRK";
                config.Copyright = "© 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.";

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(config);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Config save error: " + ex.Message);
            }
        }

        public static AppConfig CreateDefaultConfig()
        {
            AppConfig config = new AppConfig
            {
                AppName = "Zen - Masaüstü Düzenleyici",
                Creator = "Serkan ŞENTÜRK",
                Author = "Serkan ŞENTÜRK",
                Copyright = "© 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.",
                RunOnStartup = false,
                SingleClickToOpen = false,
                ZenGroups = new List<ZenGroupData>()
            };

            // Default Zen Group 1: Quick Access
            ZenGroupData group1 = new ZenGroupData
            {
                Title = "Hızlı Erişim",
                Left = 80,
                Top = 60,
                Width = 320,
                Height = 250,
                ExpandedHeight = 250,
                BackgroundColor = "#181820",
                Opacity = 0.38,
                Items = new List<ZenItem>()
            };

            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            group1.Items.Add(new ZenItem
            {
                Name = "Dosya Gezgini",
                TargetPath = Path.Combine(windir, "explorer.exe")
            });

            if (Directory.Exists(userDownloads))
            {
                group1.Items.Add(new ZenItem
                {
                    Name = "İndirilenler",
                    TargetPath = userDownloads
                });
            }

            string notepadPath = Path.Combine(windir, "notepad.exe");
            if (File.Exists(notepadPath))
            {
                group1.Items.Add(new ZenItem
                {
                    Name = "Not Defteri",
                    TargetPath = notepadPath
                });
            }

            string taskmgrPath = Path.Combine(windir, "System32", "taskmgr.exe");
            if (File.Exists(taskmgrPath))
            {
                group1.Items.Add(new ZenItem
                {
                    Name = "Görev Yöneticisi",
                    TargetPath = taskmgrPath
                });
            }

            // Default Zen Group 2: Documents & Folders
            ZenGroupData group2 = new ZenGroupData
            {
                Title = "Projeler & Dosyalar",
                Left = 430,
                Top = 60,
                Width = 320,
                Height = 250,
                ExpandedHeight = 250,
                BackgroundColor = "#181820",
                Opacity = 0.38,
                Items = new List<ZenItem>()
            };

            if (Directory.Exists(userDesktop))
            {
                group2.Items.Add(new ZenItem
                {
                    Name = "Masaüstü",
                    TargetPath = userDesktop
                });
            }

            config.ZenGroups.Add(group1);
            config.ZenGroups.Add(group2);

            SaveConfig(config);
            return config;
        }

        public static void SetStartupShortcut(bool enable)
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "Zen - Masaüstü Düzenleyici.lnk");

                if (!enable)
                {
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                    return;
                }

                // Create shortcut using WScript.Shell
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    shortcut.Description = "Zen - Masaüstü Düzenleyici (Geliştirici: Serkan ŞENTÜRK)";
                    shortcut.Save();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Startup shortcut error: " + ex.Message);
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "Zen - Masaüstü Düzenleyici.lnk");
                return File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }

        public static string BackupDirectory
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        public static string CreateBackup(AppConfig config)
        {
            try
            {
                if (config == null) return null;
                string filename = string.Format("zen_yedek_{0:yyyyMMdd_HHmmss}.json", DateTime.Now);
                string fullPath = Path.Combine(BackupDirectory, filename);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(config);
                File.WriteAllText(fullPath, json);
                return fullPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Backup error: " + ex.Message);
                return null;
            }
        }

        public static List<string> GetBackups()
        {
            List<string> list = new List<string>();
            try
            {
                if (Directory.Exists(BackupDirectory))
                {
                    list.AddRange(Directory.GetFiles(BackupDirectory, "zen_yedek_*.json"));
                    list.Sort();
                    list.Reverse(); // Newest first
                }
            }
            catch { }
            return list;
        }

        public static AppConfig RestoreBackup(string backupFilePath)
        {
            try
            {
                if (File.Exists(backupFilePath))
                {
                    string json = File.ReadAllText(backupFilePath);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig config = serializer.Deserialize<AppConfig>(json);
                    if (config != null && config.ZenGroups != null)
                    {
                        SaveConfig(config);
                        return config;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
