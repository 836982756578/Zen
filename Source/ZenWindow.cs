// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Zen
{
    public class ZenWindow : Window
    {
        public ZenGroupData Data { get; private set; }
        public bool SingleClickToOpen { get; set; }
        public event Action<ZenWindow> OnConfigChanged;
        public event Action<ZenWindow> OnDeleteRequested;
        public event Action OnAddNewGroupRequested;
        public event Action<bool> OnSingleClickOptionChanged;

        private Border _rootBorder;
        private Border _headerBorder;
        private Grid _mainGrid;
        private RowDefinition _row0;
        private RowDefinition _row1;
        private StackPanel _centerPanel;
        private StackPanel _tabPanel;
        private System.Windows.Shapes.Path _folderIcon;
        private TextBlock _emojiBlock;
        private TextBlock _titleBlock;
        private WrapPanel _itemsPanel;
        private ScrollViewer _scrollViewer;
        private StackPanel _emptyContainer;
        private TextBlock _emptyHintBlock;
        private Grid _contentAreaGrid;
        private Border _searchBorder;
        private TextBox _searchBox;
        private FileSystemWatcher _watcher;

        private string _currentSearchQuery = "";

        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            Bottom,
            BottomLeft,
            BottomRight
        }

        private ResizeDirection _currentResizeDir = ResizeDirection.None;
        private bool _isResizing = false;
        private Point _resizeStartPoint;
        private double _resizeStartLeft;
        private double _resizeStartTop;
        private double _resizeStartWidth;
        private double _resizeStartHeight;

        private Border _leftResizeBorder;
        private Border _rightResizeBorder;
        private Border _bottomResizeBorder;
        private Border _bottomLeftResizeBorder;
        private Border _bottomRightResizeGrip;

        private bool _isDraggingWindow = false;
        private bool _isTopDocked = false;
        private bool _isBottomDocked = false;
        private DispatcherTimer _autoHideTimer;
        private DispatcherTimer _desktopKeeper;

        public ZenWindow(ZenGroupData data)
        {
            Data = data;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal;

            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            TextOptions.SetTextHintingMode(this, TextHintingMode.Fixed);
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);

            Left = Data.Left;
            Top = Data.Top;
            Width = Data.Width < 180 ? 320 : Data.Width;
            Height = Data.Height < 60 ? 250 : Data.Height;
            Topmost = Data.StayOnTop;

            AllowDrop = true;
            DragOver += ZenWindow_DragOver;
            Drop += ZenWindow_Drop;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    ToggleSearch();
                    e.Handled = true;
                }
            };

            BuildUI();

            SourceInitialized += ZenWindow_SourceInitialized;
            LocationChanged += ZenWindow_LocationChanged;
            SizeChanged += ZenWindow_SizeChanged;
            Deactivated += ZenWindow_Deactivated;
            StateChanged += ZenWindow_StateChanged;
            Loaded += ZenWindow_Loaded;

            MouseEnter += ZenWindow_MouseEnter;
            MouseLeave += ZenWindow_MouseLeave;
            Closed += ZenWindow_Closed;

            // Periodic desktop layer keeper & lightweight memory trimming
            int tickCounter = 0;
            _desktopKeeper = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _desktopKeeper.Tick += (s, e) =>
            {
                if (!Data.StayOnTop && !IsActive && !_isDraggingWindow && !_isResizing)
                {
                    ApplyDesktopMode();
                }

                tickCounter++;
                if (tickCounter % 15 == 0) // Every 30 seconds
                {
                    if (!_isDraggingWindow && !_isResizing)
                    {
                        NativeMethods.TrimWorkingSet();
                    }
                }
            };
            _desktopKeeper.Start();
        }

        private void ZenWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void ZenWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE, exStyle);

            ApplyDesktopMode();
            ApplyVisualStyles();
        }

        private void ZenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyDesktopMode();
            CheckDockState();
            ApplyCollapseVisual(Data.IsCollapsed, true);

            if (Data.IsFolderPortal && !string.IsNullOrEmpty(Data.FolderPortalPath))
            {
                LoadFolderPortalItems(true);
                SetupFileSystemWatcher();
            }

            if (!string.IsNullOrEmpty(Data.SortOrder) && Data.SortOrder != "None")
            {
                SortItems(Data.SortOrder, false);
            }

            RefreshItems();

            DispatcherTimer oneShotTrim = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            oneShotTrim.Tick += (s, ev) =>
            {
                oneShotTrim.Stop();
                NativeMethods.TrimWorkingSet();
            };
            oneShotTrim.Start();
        }

        public void ApplyDesktopMode()
        {
            if (Data.StayOnTop) return;

            try
            {
                IntPtr hWnd = new WindowInteropHelper(this).Handle;
                if (hWnd != IntPtr.Zero)
                {
                    NativeMethods.SetWindowPos(
                        hWnd,
                        NativeMethods.HWND_BOTTOM,
                        0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW
                    );
                }
            }
            catch
            {
            }
        }

        private void ZenWindow_Deactivated(object sender, EventArgs e)
        {
            if (!Data.StayOnTop && !_isDraggingWindow && !_isResizing)
            {
                ApplyDesktopMode();
            }
        }

        private void BuildUI()
        {
            // Outer root container
            Grid rootContainer = new Grid
            {
                ClipToBounds = false,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            // Main visual border
            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                ClipToBounds = true,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            ApplyVisualStyles();

            _mainGrid = new Grid
            {
                ClipToBounds = true,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            _row0 = new RowDefinition();
            _row1 = new RowDefinition();
            _mainGrid.RowDefinitions.Add(_row0);
            _mainGrid.RowDefinitions.Add(_row1);

            // --- Header Bar ---
            _headerBorder = new Border
            {
                Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(90, 12, 10, 14)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0, 0, 4, 4),
                Cursor = Cursors.SizeAll,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            _headerBorder.MouseLeftButtonDown += HeaderBorder_MouseLeftButtonDown;

            Grid headerGrid = new Grid
            {
                Margin = new Thickness(4, 0, 4, 0),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            // Center: Title / Tabs / Emoji
            _centerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderHeaderCenter();
            headerGrid.Children.Add(_centerPanel);

            _headerBorder.Child = headerGrid;

            // --- Content Area ---
            _contentAreaGrid = new Grid
            {
                ClipToBounds = true,
                Margin = new Thickness(0),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            _contentAreaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _contentAreaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Slide-down Search Bar
            _searchBorder = new Border
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromArgb(190, 15, 14, 18)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Visibility = Visibility.Collapsed,
                Padding = new Thickness(8, 2, 8, 2),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock sIconText = new TextBlock
            {
                Text = "🔍",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Foreground = Brushes.White
            };
            Grid.SetColumn(sIconText, 0);
            searchGrid.Children.Add(sIconText);

            _searchBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                CaretBrush = Brushes.White
            };
            _searchBox.TextChanged += (s, e) =>
            {
                _currentSearchQuery = _searchBox.Text.Trim();
                RefreshItems();
            };
            _searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    ToggleSearch();
                }
            };
            Grid.SetColumn(_searchBox, 1);
            searchGrid.Children.Add(_searchBox);

            Button clearSearchBtn = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)),
                Cursor = Cursors.Hand,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            clearSearchBtn.Click += (s, e) => ToggleSearch();
            Grid.SetColumn(clearSearchBtn, 2);
            searchGrid.Children.Add(clearSearchBtn);

            _searchBorder.Child = searchGrid;
            Grid.SetRow(_searchBorder, 0);
            _contentAreaGrid.Children.Add(_searchBorder);

            // Scroll Viewer & Items
            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(8, 8, 8, 6),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            ApplyDarkScrollBarStyle(_scrollViewer);

            _itemsPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            _scrollViewer.Content = _itemsPanel;
            Grid.SetRow(_scrollViewer, 1);
            _contentAreaGrid.Children.Add(_scrollViewer);

            _emptyContainer = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16)
            };

            _emptyHintBlock = new TextBlock
            {
                Text = "Bu Zen alanı şu an boş.\nİçeriği gösterilecek bir klasör seçin veya dosyaları sürükleyin:",
                Foreground = new SolidColorBrush(Color.FromArgb(160, 230, 230, 240)),
                FontSize = 11.5,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _emptyContainer.Children.Add(_emptyHintBlock);

            Button pickFolderBtn = new Button
            {
                Content = "📂 Klasör Seç (İçindekileri Göster)",
                Height = 32,
                Padding = new Thickness(14, 4, 14, 4),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11.5,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            pickFolderBtn.Click += (s, e) => ShowSelectFolderDialog();
            pickFolderBtn.MouseEnter += (s, e) => pickFolderBtn.Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            pickFolderBtn.MouseLeave += (s, e) => pickFolderBtn.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            _emptyContainer.Children.Add(pickFolderBtn);

            Button addFilesBtn = new Button
            {
                Content = "📄 Dosya / Kısayol Ekle...",
                Height = 24,
                Padding = new Thickness(10, 2, 10, 2),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromArgb(180, 220, 220, 230)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11.0,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            addFilesBtn.Click += (s, e) => ShowAddFileDialog();
            addFilesBtn.MouseEnter += (s, e) => addFilesBtn.Foreground = Brushes.White;
            addFilesBtn.MouseLeave += (s, e) => addFilesBtn.Foreground = new SolidColorBrush(Color.FromArgb(180, 220, 220, 230));
            _emptyContainer.Children.Add(addFilesBtn);

            _contentAreaGrid.Children.Add(_emptyContainer);

            // Right-click menu on background
            ContextMenu bgMenu = CreateGroupContextMenu();
            _headerBorder.ContextMenu = bgMenu;
            _contentAreaGrid.ContextMenu = bgMenu;
            _mainGrid.ContextMenu = bgMenu;

            // Assemble main grid
            _row0.Height = GridLength.Auto;
            _row1.Height = new GridLength(1, GridUnitType.Star);
            Grid.SetRow(_headerBorder, 0);
            Grid.SetRow(_contentAreaGrid, 1);
            _mainGrid.Children.Add(_headerBorder);
            _mainGrid.Children.Add(_contentAreaGrid);

            _rootBorder.Child = _mainGrid;
            rootContainer.Children.Add(_rootBorder);

            // --- Multi-Edge Resize Borders ---
            _leftResizeBorder = new Border
            {
                Width = 6,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeWE
            };
            _leftResizeBorder.MouseLeftButtonDown += (s, e) => StartResize(ResizeDirection.Left, e);
            rootContainer.Children.Add(_leftResizeBorder);

            _rightResizeBorder = new Border
            {
                Width = 6,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeWE
            };
            _rightResizeBorder.MouseLeftButtonDown += (s, e) => StartResize(ResizeDirection.Right, e);
            rootContainer.Children.Add(_rightResizeBorder);

            _bottomResizeBorder = new Border
            {
                Height = 6,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeNS
            };
            _bottomResizeBorder.MouseLeftButtonDown += (s, e) => StartResize(ResizeDirection.Bottom, e);
            rootContainer.Children.Add(_bottomResizeBorder);

            _bottomLeftResizeBorder = new Border
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeNESW
            };
            _bottomLeftResizeBorder.MouseLeftButtonDown += (s, e) => StartResize(ResizeDirection.BottomLeft, e);
            rootContainer.Children.Add(_bottomLeftResizeBorder);

            _bottomRightResizeGrip = new Border
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeNWSE,
                Margin = new Thickness(0, 0, 1, 1)
            };
            Canvas gripCanvas = new Canvas { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 2, 2) };
            gripCanvas.Children.Add(new Line { X1 = 8, Y1 = 2, X2 = 2, Y2 = 8, Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), StrokeThickness = 1.2 });
            gripCanvas.Children.Add(new Line { X1 = 8, Y1 = 5, X2 = 5, Y2 = 8, Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), StrokeThickness = 1.2 });
            _bottomRightResizeGrip.Child = gripCanvas;
            _bottomRightResizeGrip.MouseLeftButtonDown += (s, e) => StartResize(ResizeDirection.BottomRight, e);
            rootContainer.Children.Add(_bottomRightResizeGrip);

            rootContainer.MouseMove += RootContainer_MouseMove;
            rootContainer.MouseLeftButtonUp += RootContainer_MouseLeftButtonUp;

            Content = rootContainer;
        }

        public void ToggleSearch()
        {
            if (_searchBorder.Visibility == Visibility.Visible)
            {
                _searchBorder.Visibility = Visibility.Collapsed;
                _searchBox.Text = "";
                _currentSearchQuery = "";
                RefreshItems();
            }
            else
            {
                _searchBorder.Visibility = Visibility.Visible;
                _searchBox.Focus();
                _searchBox.SelectAll();
            }
        }

        private void RenderHeaderCenter()
        {
            if (_centerPanel == null) return;
            _centerPanel.Children.Clear();

            if (Data.Tabs != null && Data.Tabs.Count > 1)
            {
                // Tabbed Header Mode
                if (_tabPanel == null)
                {
                    _tabPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                }
                _tabPanel.Children.Clear();

                for (int i = 0; i < Data.Tabs.Count; i++)
                {
                    int tabIdx = i;
                    ZenTabData tab = Data.Tabs[i];
                    bool isActive = (tabIdx == Data.ActiveTabIndex);

                    Border tabBorder = new Border
                    {
                        Height = 22,
                        Padding = new Thickness(10, 0, 10, 0),
                        Margin = new Thickness(2, 0, 2, 0),
                        CornerRadius = new CornerRadius(3),
                        VerticalAlignment = VerticalAlignment.Center,
                        Background = isActive ? new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)) : Brushes.Transparent,
                        BorderBrush = isActive ? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) : Brushes.Transparent,
                        BorderThickness = new Thickness(1),
                        Cursor = Cursors.Hand
                    };

                    Grid tabContent = new Grid
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    StackPanel textPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (!string.IsNullOrEmpty(tab.CustomEmoji))
                    {
                        textPanel.Children.Add(new TextBlock
                        {
                            Text = tab.CustomEmoji + " ",
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 2, 0)
                        });
                    }

                    textPanel.Children.Add(new TextBlock
                    {
                        Text = CleanTitle(tab.Title),
                        Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                        FontFamily = new FontFamily("Segoe UI"),
                        FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                        FontSize = 11.5,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    tabContent.Children.Add(textPanel);
                    tabBorder.Child = tabContent;

                    if (!isActive)
                    {
                        tabBorder.MouseEnter += (s, e) =>
                        {
                            tabBorder.Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
                        };
                        tabBorder.MouseLeave += (s, e) =>
                        {
                            tabBorder.Background = Brushes.Transparent;
                        };
                    }

                    tabBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        SwitchTab(tabIdx);
                        e.Handled = true;
                    };
                    _tabPanel.Children.Add(tabBorder);
                }

                // Add Tab button as a sleek rounded border
                Border addTabBtn = new Border
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(3, 0, 0, 0),
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    ToolTip = "Bu gruba yeni sekme ekle"
                };

                TextBlock addIcon = new TextBlock
                {
                    Text = "+",
                    Foreground = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, -1, 0, 0)
                };
                addTabBtn.Child = addIcon;

                addTabBtn.MouseEnter += (s, e) =>
                {
                    addTabBtn.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
                    addIcon.Foreground = Brushes.White;
                };
                addTabBtn.MouseLeave += (s, e) =>
                {
                    addTabBtn.Background = Brushes.Transparent;
                    addIcon.Foreground = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255));
                };
                addTabBtn.MouseLeftButtonDown += (s, e) =>
                {
                    AddNewTabPrompt();
                    e.Handled = true;
                };

                _tabPanel.Children.Add(addTabBtn);
                _centerPanel.Children.Add(_tabPanel);
            }
            else
            {
                // Single Title Mode
                Grid titleGrid = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                StackPanel titleStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                if (!string.IsNullOrEmpty(Data.CustomEmoji))
                {
                    _emojiBlock = new TextBlock
                    {
                        Text = Data.CustomEmoji,
                        FontSize = 13.0,
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    titleStack.Children.Add(_emojiBlock);
                }
                else
                {
                    _folderIcon = new System.Windows.Shapes.Path
                    {
                        Width = 15,
                        Height = 12,
                        Fill = Brushes.White,
                        Stretch = Stretch.Fill,
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Data = Geometry.Parse("M 0,2 C 0,0.9 0.9,0 2,0 L 5.5,0 L 7,2.5 L 13,2.5 C 13.8,2.5 14.5,3.2 14.5,4 L 14.5,11 C 14.5,11.8 13.8,12.5 13,12.5 L 1.5,12.5 C 0.7,12.5 0,11.8 0,11 Z")
                    };
                    titleStack.Children.Add(_folderIcon);
                }

                _titleBlock = new TextBlock
                {
                    Text = CleanTitle(Data.Title),
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                titleStack.Children.Add(_titleBlock);

                titleGrid.Children.Add(titleStack);
                _centerPanel.Children.Add(titleGrid);
            }
        }

        public void SwitchTab(int tabIndex)
        {
            if (Data.Tabs == null || tabIndex < 0 || tabIndex >= Data.Tabs.Count) return;

            if (Data.ActiveTabIndex >= 0 && Data.ActiveTabIndex < Data.Tabs.Count)
            {
                Data.Tabs[Data.ActiveTabIndex].Items = new List<ZenItem>(Data.Items);
                Data.Tabs[Data.ActiveTabIndex].FolderPortalPath = Data.FolderPortalPath;
                Data.Tabs[Data.ActiveTabIndex].IsFolderPortal = Data.IsFolderPortal;
            }

            Data.ActiveTabIndex = tabIndex;
            ZenTabData activeTab = Data.Tabs[tabIndex];

            Data.Title = activeTab.Title;
            Data.CustomEmoji = activeTab.CustomEmoji;
            Data.FolderPortalPath = activeTab.FolderPortalPath;
            Data.IsFolderPortal = activeTab.IsFolderPortal;
            Data.Items = activeTab.Items ?? new List<ZenItem>();

            if (Data.IsFolderPortal && !string.IsNullOrEmpty(Data.FolderPortalPath))
            {
                LoadFolderPortalItems(true);
                SetupFileSystemWatcher();
            }

            RenderHeaderCenter();
            RefreshItems();
            NotifyConfigChanged();
        }

        public void AddNewTabPrompt()
        {
            string title = InputDialog.Show(this, "Yeni Sekme", "Sekme için bir başlık belirleyin:", "Yeni Sekme");
            if (string.IsNullOrEmpty(title)) return;

            if (Data.Tabs == null || Data.Tabs.Count == 0)
            {
                Data.Tabs = new List<ZenTabData>
                {
                    new ZenTabData
                    {
                        Title = Data.Title,
                        CustomEmoji = Data.CustomEmoji,
                        FolderPortalPath = Data.FolderPortalPath,
                        IsFolderPortal = Data.IsFolderPortal,
                        Items = new List<ZenItem>(Data.Items)
                    }
                };
                Data.ActiveTabIndex = 0;
            }

            ZenTabData newTab = new ZenTabData
            {
                Title = title,
                Items = new List<ZenItem>()
            };
            Data.Tabs.Add(newTab);
            SwitchTab(Data.Tabs.Count - 1);
        }

        private void StartResize(ResizeDirection dir, MouseButtonEventArgs e)
        {
            if (Data.IsLocked) return;
            if (Data.IsCollapsed && dir != ResizeDirection.Left && dir != ResizeDirection.Right) return;

            _isResizing = true;
            _currentResizeDir = dir;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartLeft = Left;
            _resizeStartTop = Top;
            _resizeStartWidth = ActualWidth;
            _resizeStartHeight = ActualHeight;

            IInputElement captured = senderAsBorder(dir);
            if (captured != null)
            {
                captured.CaptureMouse();
            }
            e.Handled = true;
        }

        private IInputElement senderAsBorder(ResizeDirection dir)
        {
            switch (dir)
            {
                case ResizeDirection.Left: return _leftResizeBorder;
                case ResizeDirection.Right: return _rightResizeBorder;
                case ResizeDirection.Bottom: return _bottomResizeBorder;
                case ResizeDirection.BottomLeft: return _bottomLeftResizeBorder;
                case ResizeDirection.BottomRight: return _bottomRightResizeGrip;
                default: return null;
            }
        }

        private void RootContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizing || e.LeftButton != MouseButtonState.Pressed) return;

            Point curPos = e.GetPosition(this);
            double deltaX = curPos.X - _resizeStartPoint.X;
            double deltaY = curPos.Y - _resizeStartPoint.Y;

            switch (_currentResizeDir)
            {
                case ResizeDirection.Right:
                    double newW_R = Math.Max(200, _resizeStartWidth + deltaX);
                    Width = newW_R;
                    break;

                case ResizeDirection.Left:
                    double newW_L = Math.Max(200, _resizeStartWidth - deltaX);
                    double newL_L = _resizeStartLeft + (_resizeStartWidth - newW_L);
                    Left = newL_L;
                    Width = newW_L;
                    break;

                case ResizeDirection.Bottom:
                    if (!Data.IsCollapsed)
                    {
                        double newH_B = Math.Max(120, _resizeStartHeight + deltaY);
                        Height = newH_B;
                        Data.ExpandedHeight = newH_B;
                    }
                    break;

                case ResizeDirection.BottomRight:
                    double newW_BR = Math.Max(200, _resizeStartWidth + deltaX);
                    Width = newW_BR;
                    if (!Data.IsCollapsed)
                    {
                        double newH_BR = Math.Max(120, _resizeStartHeight + deltaY);
                        Height = newH_BR;
                        Data.ExpandedHeight = newH_BR;
                    }
                    break;

                case ResizeDirection.BottomLeft:
                    double newW_BL = Math.Max(200, _resizeStartWidth - deltaX);
                    double newL_BL = _resizeStartLeft + (_resizeStartWidth - newW_BL);
                    Left = newL_BL;
                    Width = newW_BL;
                    if (!Data.IsCollapsed)
                    {
                        double newH_BL = Math.Max(120, _resizeStartHeight + deltaY);
                        Height = newH_BL;
                        Data.ExpandedHeight = newH_BL;
                    }
                    break;
            }
        }

        private void RootContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                IInputElement captured = senderAsBorder(_currentResizeDir);
                if (captured != null)
                {
                    captured.ReleaseMouseCapture();
                }
                _currentResizeDir = ResizeDirection.None;

                Data.Left = Left;
                Data.Top = Top;
                Data.Width = Width;
                Data.Height = Height;
                if (!Data.IsCollapsed)
                {
                    Data.ExpandedHeight = Height;
                }
                NotifyConfigChanged();
            }
        }

        public void SortItems(string mode, bool notify = true)
        {
            Data.SortOrder = mode;
            if (Data.Items == null || Data.Items.Count <= 1) return;

            switch (mode)
            {
                case "NameAsc":
                    Data.Items.Sort(new Comparison<ZenItem>((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase)));
                    break;
                case "NameDesc":
                    Data.Items.Sort(new Comparison<ZenItem>((a, b) => string.Compare(b.Name, a.Name, StringComparison.CurrentCultureIgnoreCase)));
                    break;
                case "DateDesc":
                    Data.Items.Sort(new Comparison<ZenItem>((a, b) =>
                    {
                        try
                        {
                            DateTime dtA = File.Exists(a.TargetPath) ? File.GetLastWriteTime(a.TargetPath) : (Directory.Exists(a.TargetPath) ? Directory.GetLastWriteTime(a.TargetPath) : DateTime.MinValue);
                            DateTime dtB = File.Exists(b.TargetPath) ? File.GetLastWriteTime(b.TargetPath) : (Directory.Exists(b.TargetPath) ? Directory.GetLastWriteTime(b.TargetPath) : DateTime.MinValue);
                            return dtB.CompareTo(dtA);
                        }
                        catch { return 0; }
                    }));
                    break;
                case "Type":
                    Data.Items.Sort(new Comparison<ZenItem>((a, b) =>
                    {
                        bool isDirA = Directory.Exists(a.TargetPath);
                        bool isDirB = Directory.Exists(b.TargetPath);
                        if (isDirA && !isDirB) return -1;
                        if (!isDirA && isDirB) return 1;
                        string extA = System.IO.Path.GetExtension(a.TargetPath);
                        string extB = System.IO.Path.GetExtension(b.TargetPath);
                        int cmp = string.Compare(extA, extB, StringComparison.CurrentCultureIgnoreCase);
                        return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    }));
                    break;
            }

            RefreshItems();
            if (notify) NotifyConfigChanged();
        }

        private void GetScreenInfo(out Rect workArea, out Rect bounds, out double bottomEdge)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                System.Windows.Forms.Screen screen = null;

                if (hwnd != IntPtr.Zero)
                {
                    screen = System.Windows.Forms.Screen.FromHandle(hwnd);
                }

                if (screen == null)
                {
                    int midX = (int)(Left + Width / 2);
                    int midY = (int)(Top + Height / 2);
                    screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(midX, midY));
                }

                if (screen != null)
                {
                    double dpiX = 1.0;
                    double dpiY = 1.0;
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null && source.CompositionTarget != null)
                    {
                        dpiX = source.CompositionTarget.TransformToDevice.M11;
                        dpiY = source.CompositionTarget.TransformToDevice.M22;
                        if (dpiX <= 0) dpiX = 1.0;
                        if (dpiY <= 0) dpiY = 1.0;
                    }

                    workArea = new Rect(
                        screen.WorkingArea.Left / dpiX,
                        screen.WorkingArea.Top / dpiY,
                        screen.WorkingArea.Width / dpiX,
                        screen.WorkingArea.Height / dpiY
                    );

                    bounds = new Rect(
                        screen.Bounds.Left / dpiX,
                        screen.Bounds.Top / dpiY,
                        screen.Bounds.Width / dpiX,
                        screen.Bounds.Height / dpiY
                    );

                    // Determine true bottom edge:
                    // Primary monitor always has taskbar at workArea.Bottom
                    if (screen.Primary)
                    {
                        bottomEdge = workArea.Bottom;
                    }
                    else
                    {
                        // Check if a secondary taskbar is active on this monitor
                        bool hasTaskbar = false;
                        IntPtr hSec = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null);
                        if (hSec != IntPtr.Zero)
                        {
                            System.Windows.Forms.Screen secScreen = System.Windows.Forms.Screen.FromHandle(hSec);
                            if (secScreen != null && secScreen.DeviceName == screen.DeviceName)
                            {
                                hasTaskbar = true;
                            }
                        }
                        bottomEdge = hasTaskbar ? workArea.Bottom : bounds.Bottom;
                    }
                    return;
                }
            }
            catch
            {
            }

            workArea = SystemParameters.WorkArea;
            bounds = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            bottomEdge = workArea.Bottom;
        }

        private void CheckDockState()
        {
            Rect workArea, bounds;
            double bottomEdge;
            GetScreenInfo(out workArea, out bounds, out bottomEdge);

            _isTopDocked = (Top <= workArea.Top + 15);
            _isBottomDocked = (Math.Abs((Top + Height) - bottomEdge) <= 25 || (Top + Height) >= bottomEdge - 15);
        }

        private void UpdateLayoutForDocking()
        {
            if (_mainGrid == null || _headerBorder == null || _contentAreaGrid == null || _rootBorder == null) return;

            _mainGrid.Children.Clear();

            if (_isTopDocked)
            {
                _row0.Height = new GridLength(1, GridUnitType.Star);
                _row1.Height = GridLength.Auto;

                Grid.SetRow(_contentAreaGrid, 0);
                Grid.SetRow(_headerBorder, 1);

                // Top docked: top edge is flat!
                _rootBorder.CornerRadius = new CornerRadius(0, 0, 4, 4);
                _rootBorder.BorderThickness = new Thickness(1, 0, 1, 1);

                _headerBorder.CornerRadius = new CornerRadius(0, 0, 4, 4);
                _headerBorder.BorderThickness = new Thickness(0, 1, 0, 0);

                _mainGrid.Children.Add(_contentAreaGrid);
                _mainGrid.Children.Add(_headerBorder);
            }
            else if (_isBottomDocked)
            {
                _row0.Height = GridLength.Auto;
                _row1.Height = new GridLength(1, GridUnitType.Star);

                Grid.SetRow(_headerBorder, 0);
                Grid.SetRow(_contentAreaGrid, 1);

                // Bottom docked: bottom edge is 100% FLAT against screen/taskbar!
                _rootBorder.CornerRadius = new CornerRadius(4, 4, 0, 0);
                _rootBorder.BorderThickness = new Thickness(1, 1, 1, 0);

                _headerBorder.CornerRadius = new CornerRadius(4, 4, 0, 0);
                _headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);

                _mainGrid.Children.Add(_headerBorder);
                _mainGrid.Children.Add(_contentAreaGrid);
            }
            else
            {
                _row0.Height = GridLength.Auto;
                _row1.Height = new GridLength(1, GridUnitType.Star);

                Grid.SetRow(_headerBorder, 0);
                Grid.SetRow(_contentAreaGrid, 1);

                // Floating: all 4 corners rounded
                _rootBorder.CornerRadius = new CornerRadius(4);
                _rootBorder.BorderThickness = new Thickness(1);

                _headerBorder.CornerRadius = new CornerRadius(4, 4, 0, 0);
                _headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);

                _mainGrid.Children.Add(_headerBorder);
                _mainGrid.Children.Add(_contentAreaGrid);
            }
        }

        public void ApplyVisualStyles()
        {
            if (_rootBorder == null) return;

            try
            {
                Color baseColor;
                try
                {
                    baseColor = (Color)ColorConverter.ConvertFromString(string.IsNullOrEmpty(Data.BackgroundColor) ? "#181820" : Data.BackgroundColor);
                }
                catch
                {
                    baseColor = Color.FromRgb(24, 24, 32);
                }

                double opacity = Data.Opacity < 0.05 ? 0.38 : Data.Opacity;
                byte alpha = (byte)(opacity * 255);

                LinearGradientBrush lgb = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
                byte topR = (byte)Math.Min(255, baseColor.R + 6);
                byte topG = (byte)Math.Min(255, baseColor.G + 5);
                byte topB = (byte)Math.Min(255, baseColor.B + 5);
                byte botR = (byte)(baseColor.R * 0.40);
                byte botG = (byte)(baseColor.G * 0.40);
                byte botB = (byte)(baseColor.B * 0.40);

                lgb.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(alpha * 0.85), topR, topG, topB), 0.0));
                lgb.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(alpha * 0.70), baseColor.R, baseColor.G, baseColor.B), 0.35));
                lgb.GradientStops.Add(new GradientStop(Color.FromArgb((byte)Math.Min(255, alpha * 0.90), botR, botG, botB), 1.0));
                _rootBorder.Background = lgb;
                _rootBorder.BorderBrush = new SolidColorBrush(Color.FromArgb((byte)Math.Min(60, Math.Max(15, alpha * 0.35)), 255, 255, 255));

                // --- Windows 10/11 True Acrylic Blur Behind ---
                try
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        if (Data.AcrylicEnabled)
                        {
                            byte acrylicAlpha = (byte)Math.Min(170, Math.Max(25, (int)(alpha * 0.65)));
                            uint gradColor = ((uint)acrylicAlpha << 24) | ((uint)baseColor.B << 16) | ((uint)baseColor.G << 8) | (uint)baseColor.R;
                            NativeMethods.EnableBlurBehind(hwnd, gradColor);
                        }
                        else
                        {
                            NativeMethods.DisableBlurBehind(hwnd);
                        }
                    }
                }
                catch { }

                if (Data.IsCollapsed)
                {
                    _headerBorder.Background = Brushes.Transparent;
                    _headerBorder.BorderThickness = new Thickness(0);
                }
                else
                {
                    byte headerAlpha = (byte)Math.Min(200, Math.Max(30, (int)(alpha * 0.80)));
                    _headerBorder.Background = new SolidColorBrush(Color.FromArgb(headerAlpha, (byte)(baseColor.R * 0.5), (byte)(baseColor.G * 0.5), (byte)(baseColor.B * 0.5)));
                    _headerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                    _headerBorder.BorderThickness = _isTopDocked ? new Thickness(0, 1, 0, 0) : new Thickness(0, 0, 0, 1);
                }
            }
            catch
            {
                _rootBorder.Background = new SolidColorBrush(Color.FromArgb(200, 24, 24, 32));
                _rootBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            }
            UpdateLayoutForDocking();
        }

        private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleCollapse();
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Data.IsLocked) return;

                _isDraggingWindow = true;
                if (_autoHideTimer != null) _autoHideTimer.Stop();

                BeginAnimation(Window.HeightProperty, null);
                BeginAnimation(Window.TopProperty, null);

                try
                {
                    DragMove();
                }
                catch
                {
                }
                finally
                {
                    _isDraggingWindow = false;
                    OnDragMoveFinished();
                }
            }
        }

        private void OnDragMoveFinished()
        {
            Rect workArea, bounds;
            double bottomEdge;
            GetScreenInfo(out workArea, out bounds, out bottomEdge);

            double snapThreshold = 45.0; // Comfort zone for snapping

            // 1. Check Top Docking
            if (Math.Abs(Top - workArea.Top) <= snapThreshold || Top < workArea.Top)
            {
                Top = workArea.Top;
                _isTopDocked = true;
                _isBottomDocked = false;
            }
            // 2. Check Bottom Docking (snaps flush to bottomEdge with 0px gap)
            else
            {
                double winBottom = Top + Height;

                if (Math.Abs(winBottom - bottomEdge) <= snapThreshold || winBottom >= bottomEdge - 5)
                {
                    Top = bottomEdge - Height;
                    _isBottomDocked = true;
                    _isTopDocked = false;
                }
                else
                {
                    _isTopDocked = false;
                    _isBottomDocked = false;
                }
            }

            UpdateLayoutForDocking();

            Data.Left = Left;
            Data.Top = Top;
            Data.Width = Width;
            Data.Height = Height;
            if (!Data.IsCollapsed)
            {
                Data.ExpandedHeight = Height;
            }

            ApplyVisualStyles();
            ApplyDesktopMode();
            NotifyConfigChanged();
        }

        private void ZenWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isDraggingWindow || _isResizing) return;

            if (Data.AutoHideOnEdge && Data.IsCollapsed && (_isTopDocked || _isBottomDocked))
            {
                if (_autoHideTimer != null)
                {
                    _autoHideTimer.Stop();
                    _autoHideTimer = null;
                }
                ExpandRollup();
            }
        }

        private void ZenWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDraggingWindow || _isResizing) return;

            if (Data.AutoHideOnEdge && !Data.IsCollapsed && (_isTopDocked || _isBottomDocked))
            {
                if (_autoHideTimer != null) _autoHideTimer.Stop();

                _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
                _autoHideTimer.Tick += (s, ev) =>
                {
                    _autoHideTimer.Stop();
                    _autoHideTimer = null;

                    if (!IsMouseOver && !Data.IsCollapsed && !_isDraggingWindow && !_isResizing)
                    {
                        CollapseRollup();
                    }
                };
                _autoHideTimer.Start();
            }
        }

        public void ToggleCollapse()
        {
            if (Data.IsCollapsed)
            {
                ExpandRollup();
            }
            else
            {
                CollapseRollup();
            }
        }

        public void ExpandRollup()
        {
            if (!Data.IsCollapsed) return;
            Data.IsCollapsed = false;
            ApplyCollapseVisual(false);
        }

        public void CollapseRollup()
        {
            if (Data.IsCollapsed) return;
            if (ActualHeight > 40)
            {
                Data.ExpandedHeight = ActualHeight;
            }
            Data.IsCollapsed = true;
            ApplyCollapseVisual(true);
        }

        private void ApplyCollapseVisual(bool isCollapsed, bool instant = false)
        {
            double headerH = 28;
            double targetHeight = isCollapsed ? headerH : (Data.ExpandedHeight < 80 ? 250 : Data.ExpandedHeight);
            double targetTop = Top;

            if (_isBottomDocked)
            {
                Rect workArea, bounds;
                double bottomEdge;
                GetScreenInfo(out workArea, out bounds, out bottomEdge);
                targetTop = bottomEdge - targetHeight;
            }

            if (instant)
            {
                Height = targetHeight;
                if (_isBottomDocked) Top = targetTop;

                _contentAreaGrid.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                _contentAreaGrid.Opacity = 1.0;
                _contentAreaGrid.IsHitTestVisible = !isCollapsed;
                if (_bottomResizeBorder != null) _bottomResizeBorder.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                if (_bottomLeftResizeBorder != null) _bottomLeftResizeBorder.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                if (_bottomRightResizeGrip != null) _bottomRightResizeGrip.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                ApplyVisualStyles();
                if (isCollapsed) NativeMethods.TrimWorkingSet();
                return;
            }

            if (!isCollapsed)
            {
                _contentAreaGrid.Visibility = Visibility.Visible;
                _contentAreaGrid.Opacity = 1.0;
                _contentAreaGrid.IsHitTestVisible = true;
                if (_bottomResizeBorder != null) _bottomResizeBorder.Visibility = Visibility.Visible;
                if (_bottomLeftResizeBorder != null) _bottomLeftResizeBorder.Visibility = Visibility.Visible;
                if (_bottomRightResizeGrip != null) _bottomRightResizeGrip.Visibility = Visibility.Visible;
                if (_isTopDocked) _row0.Height = new GridLength(1, GridUnitType.Star);
                else _row1.Height = new GridLength(1, GridUnitType.Star);
                ApplyVisualStyles();
            }

            BeginAnimation(Window.HeightProperty, null);
            BeginAnimation(Window.TopProperty, null);

            double startHeight = ActualHeight;
            double startTop = Top;

            TimeSpan duration = TimeSpan.FromMilliseconds(isCollapsed ? 210 : 240);
            CubicEase easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            DoubleAnimation heightAnim = new DoubleAnimation
            {
                From = startHeight,
                To = targetHeight,
                Duration = duration,
                EasingFunction = easing
            };

            DoubleAnimation topAnim = null;
            if (_isBottomDocked)
            {
                topAnim = new DoubleAnimation
                {
                    From = startTop,
                    To = targetTop,
                    Duration = duration,
                    EasingFunction = easing
                };
            }

            heightAnim.Completed += (s, e) =>
            {
                BeginAnimation(Window.HeightProperty, null);
                Height = targetHeight;

                if (_isBottomDocked)
                {
                    BeginAnimation(Window.TopProperty, null);
                    Top = targetTop;
                }

                _contentAreaGrid.Opacity = 1.0;
                _contentAreaGrid.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                _contentAreaGrid.IsHitTestVisible = !isCollapsed;

                if (_bottomResizeBorder != null) _bottomResizeBorder.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                if (_bottomLeftResizeBorder != null) _bottomLeftResizeBorder.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                if (_bottomRightResizeGrip != null) _bottomRightResizeGrip.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;

                ApplyVisualStyles();
                NotifyConfigChanged();
                if (isCollapsed) NativeMethods.TrimWorkingSet();
            };

            BeginAnimation(Window.HeightProperty, heightAnim);
            if (topAnim != null)
            {
                BeginAnimation(Window.TopProperty, topAnim);
            }
        }

        private void ApplyDarkScrollBarStyle(ScrollViewer sv)
        {
            try
            {
                string xaml = @"
                <ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Style TargetType='{x:Type ScrollBar}'>
                        <Setter Property='Width' Value='6'/>
                        <Setter Property='Background' Value='Transparent'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='{x:Type ScrollBar}'>
                                    <Grid Background='Transparent'>
                                        <Track x:Name='PART_Track' IsDirectionReversed='true'>
                                            <Track.Thumb>
                                                <Thumb>
                                                    <Thumb.Template>
                                                        <ControlTemplate TargetType='{x:Type Thumb}'>
                                                            <Border Background='#55FFFFFF' CornerRadius='3' Margin='1,0,1,0'/>
                                                        </ControlTemplate>
                                                    </Thumb.Template>
                                                </Thumb>
                                            </Track.Thumb>
                                        </Track>
                                    </Grid>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                </ResourceDictionary>";
                using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xaml)))
                {
                    ResourceDictionary rd = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(ms);
                    sv.Resources.MergedDictionaries.Add(rd);
                }
            }
            catch
            {
            }
        }

        private void ZenWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!_isDraggingWindow)
            {
                Data.Left = Left;
                Data.Top = Top;
            }
        }

        private void ZenWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isDraggingWindow && !Data.IsCollapsed)
            {
                Data.Width = Width;
                Data.Height = Height;
                Data.ExpandedHeight = Height;
            }
        }

        public void RefreshItems()
        {
            _itemsPanel.Children.Clear();

            if (Data.Items == null || Data.Items.Count == 0)
            {
                if (_emptyContainer != null) _emptyContainer.Visibility = Visibility.Visible;
                return;
            }

            if (_emptyContainer != null) _emptyContainer.Visibility = Visibility.Collapsed;

            bool isList = string.Equals(Data.ViewMode, "List", StringComparison.OrdinalIgnoreCase);
            bool isMedium = string.Equals(Data.ViewMode, "Medium", StringComparison.OrdinalIgnoreCase);

            if (isList)
            {
                _itemsPanel.Orientation = Orientation.Vertical;
                _itemsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                _itemsPanel.Orientation = Orientation.Horizontal;
                _itemsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            }

            int count = 0;
            foreach (ZenItem item in Data.Items)
            {
                if (!string.IsNullOrEmpty(_currentSearchQuery))
                {
                    if (item.Name.IndexOf(_currentSearchQuery, StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                UIElement element = isList ? CreateListItemElement(item) : CreateGridItemElement(item, isMedium);
                _itemsPanel.Children.Add(element);
                count++;
            }

            if (count == 0 && !string.IsNullOrEmpty(_currentSearchQuery))
            {
                TextBlock noMatch = new TextBlock
                {
                    Text = "Sonuç bulunamadı: \"" + _currentSearchQuery + "\"",
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    FontSize = 11.5,
                    Margin = new Thickness(16),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                _itemsPanel.Children.Add(noMatch);
            }
        }

        private UIElement CreateGridItemElement(ZenItem item, bool isMedium)
        {
            double boxWidth = isMedium ? 70 : 86;
            double boxHeight = isMedium ? 76 : 92;
            double iconSize = isMedium ? 32 : 48;
            double fontSize = isMedium ? 10.5 : 11.0;
            double maxTextHeight = isMedium ? 28 : 32;

            Border itemBorder = new Border
            {
                Width = boxWidth,
                Height = boxHeight,
                Margin = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = item.TargetPath,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            StackPanel stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            Grid iconGrid = new Grid
            {
                Width = iconSize,
                Height = iconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            Image iconImg = new Image
            {
                Width = iconSize,
                Height = iconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                Source = IconHelper.GetIconForPath(item.TargetPath)
            };
            RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
            iconGrid.Children.Add(iconImg);

            if (!string.IsNullOrEmpty(item.TargetPath) &&
                (item.TargetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                 item.TargetPath.EndsWith(".url", StringComparison.OrdinalIgnoreCase)))
            {
                double badgeSize = isMedium ? 10 : 12;
                Border badge = new Border
                {
                    Width = badgeSize,
                    Height = badgeSize,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(1.5),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0),
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                };
                System.Windows.Shapes.Path arrow = new System.Windows.Shapes.Path
                {
                    Width = isMedium ? 6 : 7,
                    Height = isMedium ? 6 : 7,
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                    StrokeThickness = 1.2,
                    Data = Geometry.Parse("M 1.5,5.5 L 5.5,1.5 M 2.5,1.5 L 5.5,1.5 L 5.5,4.5"),
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                };
                badge.Child = arrow;
                iconGrid.Children.Add(badge);
            }

            stack.Children.Add(iconGrid);

            Grid textGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 3, 2, 0),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            TextBlock shadowBlock = new TextBlock
            {
                Text = item.Name,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 0, 0, 0)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = fontSize,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = maxTextHeight,
                Margin = new Thickness(1, 1, 0, 0),
                IsHitTestVisible = false,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
            textGrid.Children.Add(shadowBlock);

            TextBlock nameBlock = new TextBlock
            {
                Text = item.Name,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = fontSize,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = maxTextHeight,
                Margin = new Thickness(0, 0, 1, 1),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
            textGrid.Children.Add(nameBlock);

            stack.Children.Add(textGrid);
            itemBorder.Child = stack;

            AttachItemEvents(itemBorder, item);
            return itemBorder;
        }

        private UIElement CreateListItemElement(ZenItem item)
        {
            Border itemBorder = new Border
            {
                Height = 28,
                Margin = new Thickness(2, 1, 2, 1),
                CornerRadius = new CornerRadius(3),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = item.TargetPath,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            Grid grid = new Grid
            {
                Margin = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Image iconImg = new Image
            {
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                Source = IconHelper.GetIconForPath(item.TargetPath)
            };
            RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
            Grid.SetColumn(iconImg, 0);
            grid.Children.Add(iconImg);

            TextBlock nameBlock = new TextBlock
            {
                Text = item.Name,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(6, 0, 0, 0),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
            Grid.SetColumn(nameBlock, 1);
            grid.Children.Add(nameBlock);

            itemBorder.Child = grid;
            AttachItemEvents(itemBorder, item);
            return itemBorder;
        }

        private void AttachItemEvents(Border itemBorder, ZenItem item)
        {
            itemBorder.MouseEnter += (s, e) =>
            {
                itemBorder.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
                itemBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255));
            };
            itemBorder.MouseLeave += (s, e) =>
            {
                itemBorder.Background = Brushes.Transparent;
                itemBorder.BorderBrush = Brushes.Transparent;
            };

            itemBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (SingleClickToOpen)
                {
                    if (e.ClickCount == 1)
                    {
                        LaunchItem(item);
                        e.Handled = true;
                    }
                }
                else
                {
                    if (e.ClickCount == 2)
                    {
                        LaunchItem(item);
                        e.Handled = true;
                    }
                }
            };

            ContextMenu itemMenu = new ContextMenu();
            MenuItem runMenu = new MenuItem { Header = "🚀 Çalıştır / Aç" };
            runMenu.Click += (s, e) => LaunchItem(item);
            itemMenu.Items.Add(runMenu);

            MenuItem openLocMenu = new MenuItem { Header = "📂 Dosya Konumunu Aç" };
            openLocMenu.Click += (s, e) => OpenItemLocation(item);
            itemMenu.Items.Add(openLocMenu);

            MenuItem renameItemMenu = new MenuItem { Header = "✏️ Yeniden Adlandır" };
            renameItemMenu.Click += (s, e) => RenameItem(item);
            itemMenu.Items.Add(renameItemMenu);

            itemMenu.Items.Add(new Separator());

            MenuItem deleteItemMenu = new MenuItem { Header = "🗑️ Zen Grubundan Kaldır" };
            deleteItemMenu.Click += (s, e) =>
            {
                Data.Items.Remove(item);
                RefreshItems();
                NotifyConfigChanged();
            };
            itemMenu.Items.Add(deleteItemMenu);

            itemBorder.ContextMenu = itemMenu;
        }

        private void LaunchItem(ZenItem item)
        {
            try
            {
                if (string.IsNullOrEmpty(item.TargetPath)) return;
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = item.TargetPath,
                    Arguments = item.Arguments ?? "",
                    UseShellExecute = true
                };
                if (File.Exists(item.TargetPath))
                {
                    psi.WorkingDirectory = System.IO.Path.GetDirectoryName(item.TargetPath);
                }
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dosya açılamadı:\n" + ex.Message, "Zen - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenItemLocation(ZenItem item)
        {
            try
            {
                if (string.IsNullOrEmpty(item.TargetPath)) return;
                if (Directory.Exists(item.TargetPath))
                {
                    Process.Start("explorer.exe", item.TargetPath);
                }
                else if (File.Exists(item.TargetPath))
                {
                    Process.Start("explorer.exe", "/select,\"" + item.TargetPath + "\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Konum açılamadı:\n" + ex.Message, "Zen - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameItem(ZenItem item)
        {
            string newName = InputDialog.Show(this, "Öğeyi Yeniden Adlandır", "Öğe için yeni bir isim girin:", item.Name);
            if (!string.IsNullOrEmpty(newName))
            {
                item.Name = newName;
                RefreshItems();
                NotifyConfigChanged();
            }
        }

        private void ZenWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ZenWindow_Closed(object sender, EventArgs e)
        {
            if (_desktopKeeper != null)
            {
                _desktopKeeper.Stop();
                _desktopKeeper = null;
            }
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private void ZenWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths != null && paths.Length > 0)
                {
                    if (paths.Length == 1 && Directory.Exists(paths[0]) && (Data.Items == null || Data.Items.Count == 0))
                    {
                        MessageBoxResult result = MessageBox.Show(
                            "Bu klasörü canlı klasör portalı olarak bağlamak ister misiniz?\n\nEvet: Klasör Portalı (İçerik otomatik senkronize olur)\nHayır: Klasör kısayolu olarak ekle",
                            "Zen - Klasör Eklendi",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            SetFolderPortal(paths[0]);
                            return;
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            return;
                        }
                    }

                    foreach (string p in paths)
                    {
                        AddPathToGroup(p);
                    }
                    RefreshItems();
                    NotifyConfigChanged();
                }
            }
            e.Handled = true;
        }

        public void AddPathToGroup(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string displayName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = System.IO.Path.GetFileName(path);
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = path;
            }

            ZenItem item = new ZenItem
            {
                Name = displayName,
                TargetPath = path
            };
            Data.Items.Add(item);
        }

        public void ShowSelectFolderDialog()
        {
            try
            {
                using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fbd.Description = "İçeriği gösterilecek klasörü seçin (Klasör Portalı):";
                    fbd.ShowNewFolderButton = false;
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        SetFolderPortal(fbd.SelectedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Klasör seçilemedi: " + ex.Message, "Zen - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetFolderPortal(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            Data.FolderPortalPath = folderPath;
            Data.IsFolderPortal = true;

            string folderName = new DirectoryInfo(folderPath).Name;
            if (string.IsNullOrEmpty(Data.Title) || Data.Title == "Yeni Zen Grubu" || Data.Title == "Hızlı Erişim")
            {
                Data.Title = folderName;
                RenderHeaderCenter();
            }

            LoadFolderPortalItems(true);
            SetupFileSystemWatcher();
            RefreshItems();
            NotifyConfigChanged();
        }

        public void LoadFolderPortalItems(bool isInitialLoad)
        {
            if (string.IsNullOrEmpty(Data.FolderPortalPath) || !Directory.Exists(Data.FolderPortalPath))
            {
                return;
            }

            try
            {
                List<ZenItem> newItems = new List<ZenItem>();

                string[] dirs = Directory.GetDirectories(Data.FolderPortalPath);
                foreach (string dir in dirs)
                {
                    DirectoryInfo di = new DirectoryInfo(dir);
                    if ((di.Attributes & FileAttributes.Hidden) != 0) continue;

                    newItems.Add(new ZenItem
                    {
                        Name = di.Name,
                        TargetPath = dir
                    });
                }

                string[] files = Directory.GetFiles(Data.FolderPortalPath);
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if ((fi.Attributes & FileAttributes.Hidden) != 0) continue;
                    if (fi.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    if (fi.Name.Equals("thumbs.db", StringComparison.OrdinalIgnoreCase)) continue;

                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrEmpty(name)) name = fi.Name;

                    newItems.Add(new ZenItem
                    {
                        Name = name,
                        TargetPath = file
                    });
                }

                Data.Items = newItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FolderPortal load error: " + ex.Message);
            }
        }

        public void ReloadPortal()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke((Action)(() => ReloadPortal()));
                return;
            }

            LoadFolderPortalItems(false);
            RefreshItems();
        }

        private void SetupFileSystemWatcher()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            if (Data.IsFolderPortal && !string.IsNullOrEmpty(Data.FolderPortalPath) && Directory.Exists(Data.FolderPortalPath))
            {
                try
                {
                    _watcher = new FileSystemWatcher(Data.FolderPortalPath);
                    _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
                    _watcher.Created += (s, e) => Dispatcher.BeginInvoke((Action)(() => ReloadPortal()));
                    _watcher.Deleted += (s, e) => Dispatcher.BeginInvoke((Action)(() => ReloadPortal()));
                    _watcher.Renamed += (s, e) => Dispatcher.BeginInvoke((Action)(() => ReloadPortal()));
                    _watcher.EnableRaisingEvents = true;
                }
                catch
                {
                }
            }
        }

        private void ShowAddFileDialog()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Zen Grubuna Eklenecek Dosyayı Seçin",
                Filter = "Tüm Dosyalar (*.*)|*.*|Programlar ve Kısayollar (*.exe;*.lnk)|*.exe;*.lnk",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (string file in dlg.FileNames)
                {
                    AddPathToGroup(file);
                }
                RefreshItems();
                NotifyConfigChanged();
            }
        }

        private void ShowGroupContextMenu(Button target)
        {
            ContextMenu cm = CreateGroupContextMenu();
            cm.PlacementTarget = target;
            cm.IsOpen = true;
        }

        private ContextMenu CreateGroupContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            // 1. Search / Filter
            MenuItem searchItem = new MenuItem { Header = "🔍 Grup İçi Ara / Filtrele (Ctrl+F)" };
            searchItem.Click += (s, e) => ToggleSearch();
            menu.Items.Add(searchItem);

            // 2. View Mode
            MenuItem viewMenu = new MenuItem { Header = "👁️ Görünüm Modu" };
            MenuItem viewLarge = new MenuItem { Header = "🖼️ Büyük Simgeler (48x48)", IsCheckable = true, IsChecked = string.IsNullOrEmpty(Data.ViewMode) || Data.ViewMode == "Large" };
            viewLarge.Click += (s, e) => { Data.ViewMode = "Large"; RefreshItems(); NotifyConfigChanged(); };
            viewMenu.Items.Add(viewLarge);

            MenuItem viewMedium = new MenuItem { Header = "📦 Kompakt Simgeler (32x32)", IsCheckable = true, IsChecked = Data.ViewMode == "Medium" };
            viewMedium.Click += (s, e) => { Data.ViewMode = "Medium"; RefreshItems(); NotifyConfigChanged(); };
            viewMenu.Items.Add(viewMedium);

            MenuItem viewList = new MenuItem { Header = "📋 Liste / Ayrıntı Görünümü", IsCheckable = true, IsChecked = Data.ViewMode == "List" };
            viewList.Click += (s, e) => { Data.ViewMode = "List"; RefreshItems(); NotifyConfigChanged(); };
            viewMenu.Items.Add(viewList);
            menu.Items.Add(viewMenu);

            // 3. Sort By
            MenuItem sortMenu = new MenuItem { Header = "🔤 Sırala" };
            MenuItem sortNameAsc = new MenuItem { Header = "A ➔ Z İsme Göre" };
            sortNameAsc.Click += (s, e) => SortItems("NameAsc");
            sortMenu.Items.Add(sortNameAsc);

            MenuItem sortNameDesc = new MenuItem { Header = "Z ➔ A İsme Göre" };
            sortNameDesc.Click += (s, e) => SortItems("NameDesc");
            sortMenu.Items.Add(sortNameDesc);

            MenuItem sortDateDesc = new MenuItem { Header = "📅 Değiştirilme Tarihine Göre" };
            sortDateDesc.Click += (s, e) => SortItems("DateDesc");
            sortMenu.Items.Add(sortDateDesc);

            MenuItem sortType = new MenuItem { Header = "🏷️ Dosya / Program Türüne Göre" };
            sortType.Click += (s, e) => SortItems("Type");
            sortMenu.Items.Add(sortType);
            menu.Items.Add(sortMenu);

            // 4. Custom Emoji / Header Icon
            MenuItem emojiMenu = new MenuItem { Header = "🎨 Başlık İkonu / Emoji Seç..." };
            string[] popularEmojis = new string[] {
                "🎮 Oyunlar", "📁 Klasörler", "🚀 Programlar", "💼 İş & Belgeler",
                "🎵 Müzik & Medya", "🎬 Videolar", "📸 Görseller", "⭐ Favoriler",
                "🛠️ Araçlar", "🌐 İnternet", "💻 Bilgisayarım", "📥 İndirilenler"
            };
            foreach (string emItem in popularEmojis)
            {
                string em = emItem.Split(' ')[0];
                MenuItem mi = new MenuItem { Header = emItem };
                mi.Click += (s, e) =>
                {
                    Data.CustomEmoji = em;
                    RenderHeaderCenter();
                    NotifyConfigChanged();
                };
                emojiMenu.Items.Add(mi);
            }
            MenuItem customEm = new MenuItem { Header = "✏️ Özel Emoji Gir..." };
            customEm.Click += (s, e) =>
            {
                string res = InputDialog.Show(this, "Özel Emoji", "Başlık için bir emoji veya simge girin:", Data.CustomEmoji ?? "");
                if (res != null)
                {
                    Data.CustomEmoji = res.Trim();
                    RenderHeaderCenter();
                    NotifyConfigChanged();
                }
            };
            emojiMenu.Items.Add(customEm);

            MenuItem defaultEm = new MenuItem { Header = "❌ Varsayılan Klasör İkonu" };
            defaultEm.Click += (s, e) =>
            {
                Data.CustomEmoji = "";
                RenderHeaderCenter();
                NotifyConfigChanged();
            };
            emojiMenu.Items.Add(defaultEm);
            menu.Items.Add(emojiMenu);

            // 5. Tabs
            MenuItem tabsMenu = new MenuItem { Header = "📑 Sekmeler (Tabs)" };
            MenuItem addTabItem = new MenuItem { Header = "➕ Bu Gruba Yeni Sekme Ekle..." };
            addTabItem.Click += (s, e) => AddNewTabPrompt();
            tabsMenu.Items.Add(addTabItem);

            if (Data.Tabs != null && Data.Tabs.Count > 1)
            {
                MenuItem renameTabItem = new MenuItem { Header = "✏️ Aktif Sekmeyi Yeniden Adlandır" };
                renameTabItem.Click += (s, e) =>
                {
                    int idx = Data.ActiveTabIndex;
                    if (idx >= 0 && idx < Data.Tabs.Count)
                    {
                        string newTitle = InputDialog.Show(this, "Sekmeyi Yeniden Adlandır", "Sekme için yeni başlık girin:", Data.Tabs[idx].Title);
                        if (!string.IsNullOrEmpty(newTitle))
                        {
                            Data.Tabs[idx].Title = newTitle;
                            Data.Title = newTitle;
                            RenderHeaderCenter();
                            NotifyConfigChanged();
                        }
                    }
                };
                tabsMenu.Items.Add(renameTabItem);

                MenuItem deleteTabItem = new MenuItem { Header = "❌ Aktif Sekmeyi Sil" };
                deleteTabItem.Click += (s, e) =>
                {
                    int idx = Data.ActiveTabIndex;
                    if (idx >= 0 && idx < Data.Tabs.Count && Data.Tabs.Count > 1)
                    {
                        Data.Tabs.RemoveAt(idx);
                        int nextIdx = Math.Min(idx, Data.Tabs.Count - 1);
                        SwitchTab(nextIdx);
                    }
                };
                tabsMenu.Items.Add(deleteTabItem);
            }
            menu.Items.Add(tabsMenu);

            menu.Items.Add(new Separator());

            // Folder & File management
            MenuItem selectFolder = new MenuItem { Header = "📂 Klasör Seç (İçindekileri Göster)..." };
            selectFolder.Click += (s, e) => ShowSelectFolderDialog();
            menu.Items.Add(selectFolder);

            MenuItem addFile = new MenuItem { Header = "📄 Dosya / Kısayol Ekle..." };
            addFile.Click += (s, e) => ShowAddFileDialog();
            menu.Items.Add(addFile);

            if (Data.IsFolderPortal && !string.IsNullOrEmpty(Data.FolderPortalPath))
            {
                MenuItem refreshPortal = new MenuItem { Header = "🔄 Klasörü Yenile" };
                refreshPortal.Click += (s, e) => ReloadPortal();
                menu.Items.Add(refreshPortal);

                MenuItem openFolder = new MenuItem { Header = "📁 Klasörü Gezginde Aç" };
                openFolder.Click += (s, e) =>
                {
                    try { Process.Start("explorer.exe", "\"" + Data.FolderPortalPath + "\""); } catch { }
                };
                menu.Items.Add(openFolder);

                MenuItem detachPortal = new MenuItem { Header = "❌ Klasör Bağlantısını Kes" };
                detachPortal.Click += (s, e) =>
                {
                    Data.IsFolderPortal = false;
                    Data.FolderPortalPath = "";
                    if (_watcher != null) { _watcher.Dispose(); _watcher = null; }
                    NotifyConfigChanged();
                };
                menu.Items.Add(detachPortal);
            }

            menu.Items.Add(new Separator());

            MenuItem addGroup = new MenuItem { Header = "➕ Yeni Zen Grubu Ekle" };
            addGroup.Click += (s, e) =>
            {
                if (OnAddNewGroupRequested != null) OnAddNewGroupRequested();
            };
            menu.Items.Add(addGroup);

            MenuItem renameGroup = new MenuItem { Header = "✏️ Zen Grubunu Yeniden Adlandır" };
            renameGroup.Click += (s, e) =>
            {
                string newTitle = InputDialog.Show(this, "Zen Grubunu Yeniden Adlandır", "Zen grubu için yeni bir başlık girin:", CleanTitle(Data.Title));
                if (!string.IsNullOrEmpty(newTitle))
                {
                    Data.Title = newTitle;
                    RenderHeaderCenter();
                    NotifyConfigChanged();
                }
            };
            menu.Items.Add(renameGroup);

            MenuItem colorSettings = new MenuItem { Header = "🎨 Renk ve Saydamlık Ayarları" };
            colorSettings.Click += (s, e) =>
            {
                ColorDialog dlg = new ColorDialog(Data.BackgroundColor, Data.Opacity);
                dlg.Owner = this;
                if (dlg.ShowDialog() == true)
                {
                    Data.BackgroundColor = dlg.SelectedColorHex;
                    Data.Opacity = dlg.SelectedOpacity;
                    ApplyVisualStyles();
                    NotifyConfigChanged();
                }
            };
            menu.Items.Add(colorSettings);

            MenuItem acrylicItem = new MenuItem
            {
                Header = "🧊 Akrilik Buzlu Cam Efekti",
                IsCheckable = true,
                IsChecked = Data.AcrylicEnabled
            };
            acrylicItem.Click += (s, e) =>
            {
                Data.AcrylicEnabled = acrylicItem.IsChecked;
                ApplyVisualStyles();
                NotifyConfigChanged();
            };
            menu.Items.Add(acrylicItem);

            MenuItem autoHideItem = new MenuItem
            {
                Header = "⚡ Kenara Yaslanınca Otomatik Aç/Kapan",
                IsCheckable = true,
                IsChecked = Data.AutoHideOnEdge
            };
            autoHideItem.Click += (s, e) =>
            {
                Data.AutoHideOnEdge = autoHideItem.IsChecked;
                NotifyConfigChanged();
            };
            menu.Items.Add(autoHideItem);

            MenuItem singleClickItem = new MenuItem
            {
                Header = "🖱️ Tek Tıklamayla Aç",
                IsCheckable = true,
                IsChecked = SingleClickToOpen,
                ToolTip = "İşaretlenirse dosyalar tek tıklamayla, işaret kaldırılırsa çift tıklamayla açılır."
            };
            singleClickItem.Click += (s, e) =>
            {
                SingleClickToOpen = singleClickItem.IsChecked;
                if (OnSingleClickOptionChanged != null)
                {
                    OnSingleClickOptionChanged(SingleClickToOpen);
                }
            };
            menu.Items.Add(singleClickItem);

            MenuItem toggleCollapse = new MenuItem { Header = Data.IsCollapsed ? "🔽 Zen Grubunu Genişlet" : "🔼 Zen Grubunu Katla (Roll-up)" };
            toggleCollapse.Click += (s, e) => ToggleCollapse();
            menu.Items.Add(toggleCollapse);

            MenuItem stayOnTop = new MenuItem { Header = "📌 Her Zaman Üstte Tut", IsCheckable = true, IsChecked = Data.StayOnTop };
            stayOnTop.Click += (s, e) =>
            {
                Data.StayOnTop = stayOnTop.IsChecked;
                Topmost = Data.StayOnTop;
                ApplyDesktopMode();
                NotifyConfigChanged();
            };
            menu.Items.Add(stayOnTop);

            MenuItem lockPos = new MenuItem { Header = "🔒 Konumu ve Boyutu Kilitle", IsCheckable = true, IsChecked = Data.IsLocked };
            lockPos.Click += (s, e) =>
            {
                Data.IsLocked = lockPos.IsChecked;
                NotifyConfigChanged();
            };
            menu.Items.Add(lockPos);

            menu.Items.Add(new Separator());

            MenuItem aboutZen = new MenuItem { Header = "ℹ️ Zen Fence Hakkında (Serkan ŞENTÜRK)" };
            aboutZen.Click += (s, e) =>
            {
                MessageBox.Show(
                    "Zen Fence - Masaüstü Düzenleyici v1.0\n\n" +
                    "🌟 Geliştirici: Serkan ŞENTÜRK\n" +
                    "Telif Hakkı: © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.\n\n" +
                    "Bu uygulama Serkan ŞENTÜRK tarafından bağımsız olarak geliştirilmiştir.\n\n" +
                    "• 100% Bağımsız ve Taşınabilir (Zero-Install Portable)\n" +
                    "• Akıllı Kenar Katlanması (Zen Roll-up)\n" +
                    "• Canlı Klasör Portalı (Folder Portal)\n" +
                    "• Gerçek Akrilik Buzlu Cam Efekti\n" +
                    "• Sekmeli Gruplar & Hızlı Arama\n" +
                    "• Ultra Düşük Bellek Tüketimi (~5 MB)",
                    "Zen Fence - Masaüstü Düzenleyici",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            menu.Items.Add(aboutZen);

            MenuItem deleteGroup = new MenuItem { Header = "🗑️ Bu Zen Grubunu Sil" };
            deleteGroup.Click += (s, e) =>
            {
                MessageBoxResult res = MessageBox.Show("'" + Data.Title + "' adlı Zen grubunu silmek istediğinizden emin misiniz?", "Zen Grubunu Sil", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    if (OnDeleteRequested != null) OnDeleteRequested(this);
                }
            };
            menu.Items.Add(deleteGroup);

            return menu;
        }

        private void NotifyConfigChanged()
        {
            if (OnConfigChanged != null)
            {
                OnConfigChanged(this);
            }
        }

        private string CleanTitle(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Yeni Zen Grubu";
            string t = raw.Trim();
            int i = 0;
            while (i < t.Length && (char.IsSurrogate(t[i]) || char.IsSymbol(t[i]) || t[i] == '?' || t[i] == ' ' || t[i] == '\t'))
            {
                i++;
            }
            if (i > 0 && i < t.Length)
            {
                t = t.Substring(i).Trim();
            }
            return string.IsNullOrEmpty(t) ? "Yeni Zen Grubu" : t;
        }
    }
}
