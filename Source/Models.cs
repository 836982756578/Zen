// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;

namespace Zen
{
    public class ZenItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TargetPath { get; set; }
        public string Arguments { get; set; }

        public ZenItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "Yeni Öğe";
            TargetPath = "";
            Arguments = "";
        }
    }

    public class ZenTabData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string CustomEmoji { get; set; }
        public string FolderPortalPath { get; set; }
        public bool IsFolderPortal { get; set; }
        public List<ZenItem> Items { get; set; }

        public ZenTabData()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "Yeni Sekme";
            CustomEmoji = "";
            FolderPortalPath = "";
            IsFolderPortal = false;
            Items = new List<ZenItem>();
        }
    }

    public class ZenGroupData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string CustomEmoji { get; set; }
        public string ViewMode { get; set; } // "Large" (48x48), "Medium" (32x32), "List"
        public string SortOrder { get; set; } // "None", "NameAsc", "NameDesc", "DateDesc", "Type"
        public bool AcrylicEnabled { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double ExpandedHeight { get; set; }
        public bool IsCollapsed { get; set; }
        public bool StayOnTop { get; set; }
        public bool AutoHideOnEdge { get; set; }
        public string BackgroundColor { get; set; }
        public double Opacity { get; set; }
        public string FolderPortalPath { get; set; }
        public bool IsFolderPortal { get; set; }
        public List<ZenItem> Items { get; set; }
        public List<ZenTabData> Tabs { get; set; }
        public int ActiveTabIndex { get; set; }

        public ZenGroupData()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "Yeni Zen Grubu";
            CustomEmoji = "";
            ViewMode = "Large";
            SortOrder = "None";
            AcrylicEnabled = true;
            Left = 100;
            Top = 100;
            Width = 340;
            Height = 280;
            ExpandedHeight = 280;
            IsCollapsed = false;
            StayOnTop = false;
            AutoHideOnEdge = true;
            BackgroundColor = "#181820";
            Opacity = 0.38;
            FolderPortalPath = "";
            IsFolderPortal = false;
            Items = new List<ZenItem>();
            Tabs = new List<ZenTabData>();
            ActiveTabIndex = 0;
        }
    }

    public class AppConfig
    {
        public string AppName { get; set; }
        public string Creator { get; set; }
        public string Author { get; set; }
        public string Copyright { get; set; }
        public bool RunOnStartup { get; set; }
        public bool SingleClickToOpen { get; set; }
        public bool DesktopDoubleClickToggle { get; set; }
        public string DesktopDoubleClickAction { get; set; } // "ToggleDesktopIcons", "ToggleZenGroups", "ToggleBoth"
        public bool AutoOrganizeEnabled { get; set; }
        public List<ZenGroupData> ZenGroups { get; set; }

        public AppConfig()
        {
            AppName = "Zen - Masaüstü Düzenleyici";
            Creator = "Serkan ŞENTÜRK";
            Author = "Serkan ŞENTÜRK";
            Copyright = "© 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.";
            RunOnStartup = false;
            SingleClickToOpen = false;
            DesktopDoubleClickToggle = true;
            DesktopDoubleClickAction = "ToggleDesktopIcons";
            AutoOrganizeEnabled = false;
            ZenGroups = new List<ZenGroupData>();
        }
    }
}
