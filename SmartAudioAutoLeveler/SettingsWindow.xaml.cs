using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsManager _settingsManager;
        private readonly AudioManager _audioManager;
        private bool _isInitializing = true;

        public string SettingsPath => _settingsManager.GetSettingsFilePath();

        public SettingsWindow()
        {
            InitializeComponent();
            
            _settingsManager = SettingsManager.Instance;
            _audioManager = App.Current.MainWindow is MainWindow mainWindow ? mainWindow.AudioManager : null;
            
            InitializeWindow();
            LoadSettings();
            RefreshActiveSessions();
            
            _isInitializing = false;
        }

        /// <summary>
        /// Initializes the window properties and theme
        /// </summary>
        private void InitializeWindow()
        {
            // Set data context for binding
            DataContext = this;
            
            // Apply current theme
            ApplyTheme(_settingsManager.Settings.Theme);
            
            // Update theme button text
            UpdateThemeButtonText();
        }

        /// <summary>
        /// Loads current settings into the UI controls
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Volume settings
                BackgroundVolumeSlider.Value = _settingsManager.Settings.BackgroundVolume * 100;
                VolumeValueText.Text = $"{BackgroundVolumeSlider.Value:F0}%";

                // General settings
                AutostartCheckBox.IsChecked = _settingsManager.Settings.Autostart;
                MinimizeToTrayCheckBox.IsChecked = _settingsManager.Settings.MinimizeToTray;
                StartMinimizedCheckBox.IsChecked = _settingsManager.Settings.StartMinimized;
                EnableNotificationsCheckBox.IsChecked = _settingsManager.Settings.EnableNotifications;

                // Excluded apps
                RefreshExcludedAppsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refreshes the excluded apps list
        /// </summary>
        private void RefreshExcludedAppsList()
        {
            ExcludedAppsListBox.Items.Clear();
            foreach (var app in _settingsManager.Settings.ExcludedApps)
            {
                ExcludedAppsListBox.Items.Add(app);
            }
        }

        /// <summary>
        /// Refreshes the active audio sessions list
        /// </summary>
        private void RefreshActiveSessions()
        {
            try
            {
                if (_audioManager != null)
                {
                    var sessions = _audioManager.GetActiveSessions();
                    ActiveSessionsListView.ItemsSource = sessions.Values.ToList();
                }
                else
                {
                    ActiveSessionsListView.ItemsSource = new List<AudioSessionInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing sessions: {ex.Message}");
                ActiveSessionsListView.ItemsSource = new List<AudioSessionInfo>();
            }
        }

        /// <summary>
        /// Applies the specified theme to the window
        /// </summary>
        private void ApplyTheme(string theme)
        {
            try
            {
                var resources = Resources;
                
                switch (theme.ToLower())
                {
                    case "light":
                        resources["WindowBackgroundBrush"] = resources["LightBackgroundBrush"];
                        resources["SurfaceBrush"] = resources["LightSurfaceBrush"];
                        resources["BorderBrush"] = resources["LightBorderBrush"];
                        resources["TextBrush"] = resources["LightTextBrush"];
                        resources["SecondaryTextBrush"] = resources["LightSecondaryTextBrush"];
                        break;
                        
                    case "dark":
                        resources["WindowBackgroundBrush"] = resources["DarkBackgroundBrush"];
                        resources["SurfaceBrush"] = resources["DarkSurfaceBrush"];
                        resources["BorderBrush"] = resources["DarkBorderBrush"];
                        resources["TextBrush"] = resources["DarkTextBrush"];
                        resources["SecondaryTextBrush"] = resources["DarkSecondaryTextBrush"];
                        break;
                        
                    default: // System
                        // Use dynamic system colors (already set in XAML)
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the theme toggle button text
        /// </summary>
        private void UpdateThemeButtonText()
        {
            var currentTheme = _settingsManager.Settings.Theme.ToLower();
            ThemeToggleButton.Content = currentTheme switch
            {
                "light" => "🌙",
                "dark" => "☀️",
                _ => "🌙" // System
            };
        }

        #region Event Handlers

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTheme = _settingsManager.Settings.Theme.ToLower();
            string newTheme;
            
            switch (currentTheme)
            {
                case "light":
                    newTheme = "dark";
                    break;
                case "dark":
                    newTheme = "system";
                    break;
                default:
                    newTheme = "light";
                    break;
            }
            
            _settingsManager.Settings.Theme = newTheme;
            _settingsManager.SaveSettings();
            
            ApplyTheme(newTheme);
            UpdateThemeButtonText();
        }

        private void BackgroundVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            
            var volume = e.NewValue / 100.0f;
            VolumeValueText.Text = $"{e.NewValue:F0}%";
            
            // Update settings and audio manager
            _settingsManager.UpdateBackgroundVolume(volume);
            
            if (_audioManager != null)
            {
                _audioManager.BackgroundVolume = volume;
            }
        }

        private void AddAppButton_Click(object sender, RoutedEventArgs e)
        {
            var appName = NewAppTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(appName))
            {
                MessageBox.Show("Please enter an application name.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (_settingsManager.Settings.ExcludedApps.Contains(appName))
            {
                MessageBox.Show("This application is already in the exclusion list.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            _settingsManager.AddExcludedApp(appName);
            NewAppTextBox.Clear();
            RefreshExcludedAppsList();
        }

        private void RemoveAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string appName)
            {
                _settingsManager.RemoveExcludedApp(appName);
                RefreshExcludedAppsList();
            }
        }

        private void AutostartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.Autostart = true;
            _settingsManager.UpdateAutostart(true);
        }

        private void AutostartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.Autostart = false;
            _settingsManager.UpdateAutostart(false);
        }

        private void MinimizeToTrayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.MinimizeToTray = true;
            _settingsManager.SaveSettings();
        }

        private void MinimizeToTrayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.MinimizeToTray = false;
            _settingsManager.SaveSettings();
        }

        private void StartMinimizedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.StartMinimized = true;
            _settingsManager.SaveSettings();
        }

        private void StartMinimizedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.StartMinimized = false;
            _settingsManager.SaveSettings();
        }

        private void EnableNotificationsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.EnableNotifications = true;
            _settingsManager.SaveSettings();
        }

        private void EnableNotificationsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settingsManager.Settings.EnableNotifications = false;
            _settingsManager.SaveSettings();
        }

        private void RefreshSessionsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshActiveSessions();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to their default values? This action cannot be undone.",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            
            if (result == MessageBoxResult.Yes)
            {
                _settingsManager.ResetToDefaults();
                LoadSettings();
                RefreshActiveSessions();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settingsManager.SaveSettings();
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    /// <summary>
    /// Value converter for boolean to status text
    /// </summary>
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isForeground)
            {
                return isForeground ? "Foreground" : "Background";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Value converter for boolean to status color
    /// </summary>
    public class BoolToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isForeground)
            {
                return isForeground ? Brushes.Green : Brushes.Gray;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
