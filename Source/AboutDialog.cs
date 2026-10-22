// Zen Fence - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Zen
{
    public class AboutDialog : Window
    {
        public const string DeveloperGitHubUrl = "https://github.com/836982756578";
        public const string ProjectGitHubUrl = "https://github.com/836982756578/Zen";

        public AboutDialog()
        {
            Title = "Zen Fence Hakkında";
            Width = 460;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(248, 22, 22, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(22),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Opacity = 0.65
                }
            };

            rootBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };

            StackPanel stack = new StackPanel();

            // Header (Icon + Title + Close Button)
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock logoBlock = new TextBlock
            {
                Text = "☯️",
                FontSize = 26,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(logoBlock, 0);

            StackPanel titleGroup = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock titleBlock = new TextBlock
            {
                Text = "Zen Fence - Masaüstü Düzenleyici",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.White
            };
            TextBlock versionBlock = new TextBlock
            {
                Text = "Sürüm: v1.0.1  •  Geliştirici: Serkan ŞENTÜRK",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 165, 180)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleGroup.Children.Add(titleBlock);
            titleGroup.Children.Add(versionBlock);
            Grid.SetColumn(titleGroup, 1);

            Button closeX = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top
            };
            closeX.Click += (s, e) => Close();
            Grid.SetColumn(closeX, 2);

            headerGrid.Children.Add(logoBlock);
            headerGrid.Children.Add(titleGroup);
            headerGrid.Children.Add(closeX);
            stack.Children.Add(headerGrid);

            // Separator
            stack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(0, 14, 0, 12)
            });

            // Developer Card with GitHub Links
            Border devCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };
            StackPanel devStack = new StackPanel();

            TextBlock devHeader = new TextBlock
            {
                Text = "🌟 Geliştirici & Telif Hakkı",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 255)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            TextBlock devCopyright = new TextBlock
            {
                Text = "Telif Hakkı: © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 200)),
                Margin = new Thickness(0, 0, 0, 6)
            };

            // Clickable GitHub Links
            TextBlock githubLinks = new TextBlock
            {
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(90, 180, 255)),
                Margin = new Thickness(0, 2, 0, 0)
            };

            Hyperlink devLink = new Hyperlink(new Run("🌐 GitHub Geliştirici Sayfası: github.com/836982756578"))
            {
                Foreground = new SolidColorBrush(Color.FromRgb(95, 185, 255)),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            devLink.Click += (s, e) => OpenUrl(DeveloperGitHubUrl);

            Hyperlink repoLink = new Hyperlink(new Run("⭐ GitHub Proje Deposu: github.com/836982756578/Zen"))
            {
                Foreground = new SolidColorBrush(Color.FromRgb(95, 185, 255)),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            repoLink.Click += (s, e) => OpenUrl(ProjectGitHubUrl);

            githubLinks.Inlines.Add(devLink);
            githubLinks.Inlines.Add(new LineBreak());
            githubLinks.Inlines.Add(repoLink);

            devStack.Children.Add(devHeader);
            devStack.Children.Add(devCopyright);
            devStack.Children.Add(githubLinks);
            devCard.Child = devStack;
            stack.Children.Add(devCard);

            // Description
            TextBlock descBlock = new TextBlock
            {
                Text = "Bu uygulama Serkan ŞENTÜRK tarafından masaüstü simgelerini düzenlemek, canlı klasör portalları oluşturmak ve kenarlara katlanabilir alanlar sağlamak için bağımsız olarak geliştirilmiştir.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 205, 215)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(descBlock);

            // Features list
            string[] features = new string[]
            {
                "• 100% Bağımsız ve Taşınabilir (Zero-Install Portable)",
                "• Akıllı Kenar Katlanması (Zen Roll-up)",
                "• Canlı Klasör Portalı (Folder Portal)",
                "• Gerçek Akrilik Buzlu Cam Efekti",
                "• Masaüstüne Çift Tıklamayla Simgeleri Gizleme",
                "• Sekmeli Gruplar & Hızlı Arama (Ctrl+F)",
                "• Otomatik Yeni Sürüm Bildirimi (Update Checker)",
                "• Ultra Düşük Bellek Tüketimi (~5 MB)"
            };

            StackPanel featuresList = new StackPanel { Margin = new Thickness(4, 0, 0, 14) };
            foreach (string f in features)
            {
                featuresList.Children.Add(new TextBlock
                {
                    Text = f,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(165, 170, 185)),
                    Margin = new Thickness(0, 1.5, 0, 1.5)
                });
            }
            stack.Children.Add(featuresList);

            // Action Buttons
            Grid buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });

            Button btnDev = CreateModernButton("🌐 Geliştirici", Color.FromRgb(36, 42, 58), Color.FromRgb(95, 185, 255));
            btnDev.Click += (s, e) => OpenUrl(DeveloperGitHubUrl);
            Grid.SetColumn(btnDev, 0);

            Button btnRepo = CreateModernButton("⭐ GitHub Proje", Color.FromRgb(36, 42, 58), Color.FromRgb(95, 185, 255));
            btnRepo.Click += (s, e) => OpenUrl(ProjectGitHubUrl);
            Grid.SetColumn(btnRepo, 2);

            Button btnClose = CreateModernButton("Kapat", Color.FromRgb(55, 60, 75), Colors.White);
            btnClose.Click += (s, e) => Close();
            Grid.SetColumn(btnClose, 4);

            buttonGrid.Children.Add(btnDev);
            buttonGrid.Children.Add(btnRepo);
            buttonGrid.Children.Add(btnClose);
            stack.Children.Add(buttonGrid);

            rootBorder.Child = stack;
            Content = rootBorder;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape || e.Key == Key.Enter)
                    Close();
            };
        }

        private Button CreateModernButton(string text, Color bg, Color fg)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 32,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(fg),
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Rounded template
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

            FrameworkElementFactory contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;
            btn.Template = template;

            return btn;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
