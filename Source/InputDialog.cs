// Zen - Masaüstü Düzenleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Zen
{
    public class InputDialog : Window
    {
        public string InputText { get; private set; }
        private TextBox _textBox;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            Title = title;
            Width = 380;
            Height = 190;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 26, 26, 32)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 20,
                    ShadowDepth = 4,
                    Opacity = 0.6
                }
            };

            Grid mainGrid = new Grid { Margin = new Thickness(18) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(titleBlock, 0);
            mainGrid.Children.Add(titleBlock);

            TextBlock promptBlock = new TextBlock
            {
                Text = prompt,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(promptBlock, 1);
            mainGrid.Children.Add(promptBlock);

            _textBox = new TextBox
            {
                Text = defaultValue,
                FontSize = 13,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(150, 40, 40, 50)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 120, 120, 140)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            _textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Confirm();
                }
                else if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
            Grid.SetRow(_textBox, 2);
            mainGrid.Children.Add(_textBox);

            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button cancelBtn = CreateButton("İptal", "#353540", () =>
            {
                DialogResult = false;
                Close();
            });
            cancelBtn.Margin = new Thickness(0, 0, 10, 0);

            Button okBtn = CreateButton("Tamam", "#0078D7", () =>
            {
                Confirm();
            });

            buttonsPanel.Children.Add(cancelBtn);
            buttonsPanel.Children.Add(okBtn);
            Grid.SetRow(buttonsPanel, 3);
            mainGrid.Children.Add(buttonsPanel);

            rootBorder.Child = mainGrid;
            Content = rootBorder;

            Loaded += (s, e) =>
            {
                _textBox.Focus();
                _textBox.SelectAll();
            };

            MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
        }

        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(_textBox.Text))
            {
                return;
            }
            InputText = _textBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private Button CreateButton(string text, string hexBg, Action onClick)
        {
            Button btn = new Button
            {
                Content = text,
                Width = 85,
                Height = 30,
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Background = (Brush)new BrushConverter().ConvertFromString(hexBg),
                BorderThickness = new Thickness(0)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        public static string Show(Window owner, string title, string prompt, string defaultValue = "")
        {
            InputDialog dlg = new InputDialog(title, prompt, defaultValue);
            if (owner != null)
            {
                dlg.Owner = owner;
            }
            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                return dlg.InputText;
            }
            return null;
        }
    }
}
