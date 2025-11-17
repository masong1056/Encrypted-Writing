using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace CaesarCipherApp
{
    public partial class ConverterWindow : Window
    {
        private const int CAESAR_SHIFT = 7; // Caesar cipher shift amount
        private bool isDarkMode = false;

        public ConverterWindow()
        {
            InitializeComponent();
            ApplyWindowsTheme();

            // Listen for Windows theme changes
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            Closing += ConverterWindow_Closing;
        }

        private void ConverterWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Dispatcher.Invoke(() => ApplyWindowsTheme());
            }
        }

        private void ApplyWindowsTheme()
        {
            isDarkMode = IsWindowsDarkMode();

            if (isDarkMode)
            {
                // Dark mode colors
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                TitleText.Foreground = new SolidColorBrush(Colors.White);
                InputLabel.Foreground = new SolidColorBrush(Colors.White);
                OutputLabel.Foreground = new SolidColorBrush(Colors.White);
                EncodeRadio.Foreground = new SolidColorBrush(Colors.White);
                DecodeRadio.Foreground = new SolidColorBrush(Colors.White);

                InputTextBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                InputTextBox.Foreground = new SolidColorBrush(Colors.White);
                InputTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

                OutputTextBox.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                OutputTextBox.Foreground = new SolidColorBrush(Colors.White);
                OutputTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

                ClearButton.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                ClearButton.Foreground = new SolidColorBrush(Colors.White);
                ClearButton.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

                CopyButton.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                CopyButton.Foreground = new SolidColorBrush(Colors.White);
                CopyButton.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            }
            else
            {
                // Light mode colors
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                TitleText.Foreground = new SolidColorBrush(Colors.Black);
                InputLabel.Foreground = new SolidColorBrush(Colors.Black);
                OutputLabel.Foreground = new SolidColorBrush(Colors.Black);
                EncodeRadio.Foreground = new SolidColorBrush(Colors.Black);
                DecodeRadio.Foreground = new SolidColorBrush(Colors.Black);

                InputTextBox.Background = new SolidColorBrush(Colors.White);
                InputTextBox.Foreground = new SolidColorBrush(Colors.Black);
                InputTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

                OutputTextBox.Background = new SolidColorBrush(Color.FromRgb(249, 249, 249));
                OutputTextBox.Foreground = new SolidColorBrush(Colors.Black);
                OutputTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

                ClearButton.Background = new SolidColorBrush(Colors.White);
                ClearButton.Foreground = new SolidColorBrush(Colors.Black);
                ClearButton.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

                CopyButton.Background = new SolidColorBrush(Colors.White);
                CopyButton.Foreground = new SolidColorBrush(Colors.Black);
                CopyButton.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
        }

        private bool IsWindowsDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value is int i && i == 0;
                }
            }
            catch
            {
                return false; // Default to light mode if we can't read the registry
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter some text to convert.", "Input Required",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int shift = EncodeRadio.IsChecked == true ? CAESAR_SHIFT : -CAESAR_SHIFT;
            OutputTextBox.Text = CaesarCipher(input, shift);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Clear();
            OutputTextBox.Clear();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(OutputTextBox.Text))
            {
                Clipboard.SetText(OutputTextBox.Text);
                MessageBox.Show("Copied to clipboard!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string CaesarCipher(string text, int shift)
        {
            return new string(text.Select(c =>
            {
                if (!char.IsLetter(c))
                    return c;

                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                int offset = c - baseChar;
                int shifted = (offset + shift + 26) % 26;
                return (char)(baseChar + shifted);
            }).ToArray());
        }
    }
}