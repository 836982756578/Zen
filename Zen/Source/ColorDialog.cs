// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Zen
{
    public class ColorDialog : Window
    {
        public string SelectedColorHex { get; private set; }
        public double SelectedOpacity { get; private set; }

        private Slider _opacitySlider;
        private TextBlock _opacityValueText;
        private Border _previewBorder;

        public ColorDialog(string currentColorHex, double currentOpacity)
        {
            SelectedColorHex = string.IsNullOrEmpty(currentColorHex) ? "#181820" : currentColorHex;
            SelectedOpacity = currentOpacity < 0.1 ? 0.38 : currentOpacity;

            Title = "Renk ve Görünüm Ayarları";
            Width = 420;
            Height = 360;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(245, 24, 24, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 5,
                    Opacity = 0.65
                }
            };

            StackPanel stack = new StackPanel { Margin = new Thickness(20) };

            // Title
            TextBlock titleBlock = new TextBlock
            {
                Text = "Zen Grubu Görünümü ve Renk",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stack.Children.Add(titleBlock);

            // Preview Box
            TextBlock previewLabel = new TextBlock
            {
                Text = "Önizleme:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(previewLabel);

            _previewBorder = new Border
            {
                Height = 55,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            TextBlock previewText = new TextBlock
            {
                Text = "Örnek Zen Görünümü (Yarı Saydam)",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };
            _previewBorder.Child = previewText;
            stack.Children.Add(_previewBorder);

            // Color Presets
            TextBlock colorsLabel = new TextBlock
            {
                Text = "Renk Teması Seçin:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(colorsLabel);

            WrapPanel colorGrid = new WrapPanel { Margin = new Thickness(0, 0, 0, 15) };
            string[] presets = new string[]
            {
                "#332931", "#181820", "#121E2F", "#10261C",
                "#22122C", "#25252A", "#1B2A4A", "#2E151B"
            };

            foreach (string hex in presets)
            {
                Button cbtn = new Button
                {
                    Width = 36,
                    Height = 36,
                    Margin = new Thickness(4),
                    Background = (Brush)new BrushConverter().ConvertFromString(hex),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                string currentHex = hex;
                cbtn.Click += (s, e) =>
                {
                    SelectedColorHex = currentHex;
                    UpdatePreview();
                };
                colorGrid.Children.Add(cbtn);
            }
            stack.Children.Add(colorGrid);

            // Opacity slider
            Grid opacityGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            opacityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            opacityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            opacityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock opacityLabel = new TextBlock
            {
                Text = "Opaklık: ",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(opacityLabel, 0);
            opacityGrid.Children.Add(opacityLabel);

            _opacitySlider = new Slider
            {
                Minimum = 0.10,
                Maximum = 1.0,
                Value = SelectedOpacity,
                TickFrequency = 0.05,
                IsSnapToTickEnabled = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            _opacityValueText = new TextBlock
            {
                Text = ((int)(SelectedOpacity * 100)) + "%",
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Width = 40
            };
            _opacitySlider.ValueChanged += (s, e) =>
            {
                SelectedOpacity = e.NewValue;
                _opacityValueText.Text = ((int)(SelectedOpacity * 100)) + "%";
                UpdatePreview();
            };
            Grid.SetColumn(_opacitySlider, 1);
            Grid.SetColumn(_opacityValueText, 2);
            opacityGrid.Children.Add(_opacitySlider);
            opacityGrid.Children.Add(_opacityValueText);
            stack.Children.Add(opacityGrid);

            // Action Buttons
            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button cancelBtn = new Button
            {
                Content = "İptal",
                Width = 85,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Background = (Brush)new BrushConverter().ConvertFromString("#353540"),
                BorderThickness = new Thickness(0)
            };
            cancelBtn.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            Button okBtn = new Button
            {
                Content = "Uygula",
                Width = 85,
                Height = 32,
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Background = (Brush)new BrushConverter().ConvertFromString("#0078D7"),
                BorderThickness = new Thickness(0)
            };
            okBtn.Click += (s, e) =>
            {
                DialogResult = true;
                Close();
            };

            buttonsPanel.Children.Add(cancelBtn);
            buttonsPanel.Children.Add(okBtn);
            stack.Children.Add(buttonsPanel);

            rootBorder.Child = stack;
            Content = rootBorder;

            MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                Color baseColor = (Color)ColorConverter.ConvertFromString(SelectedColorHex);
                byte alpha = (byte)(SelectedOpacity * 255);
                _previewBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
            }
            catch
            {
            }
        }
    }
}
