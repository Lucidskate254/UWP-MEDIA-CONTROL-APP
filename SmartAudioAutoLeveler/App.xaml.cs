using System;
using System.Windows;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Initialize settings manager
                var settings = SettingsManager.Instance;
                
                // Apply theme if specified
                if (!string.IsNullOrEmpty(settings.Settings.Theme))
                {
                    ApplyTheme(settings.Settings.Theme);
                }
                
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during application startup: {ex.Message}", 
                              "Startup Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Save any pending settings
                SettingsManager.Instance.SaveSettings();
                
                base.OnExit(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during application exit: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the specified theme to the application
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
    }
}
