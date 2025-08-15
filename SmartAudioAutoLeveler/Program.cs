using System;
using System.Windows;
using System.Threading;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Main application entry point
    /// </summary>
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Set up application
                var app = new Application();
                
                // Configure application properties
                app.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                
                // Handle unhandled exceptions
                app.DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                
                // Check if we should start minimized
                var settings = SettingsManager.Instance.Settings;
                if (settings.StartMinimized)
                {
                    app.Startup += OnAppStartup;
                }
                
                // Run the application
                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error starting application: {ex.Message}", 
                              "Critical Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in the UI thread
        /// </summary>
        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\n" +
                              "The application will continue to run, but some features may not work correctly.",
                              "Unexpected Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                
                e.Handled = true;
            }
            catch
            {
                // If we can't show a message box, just log the error
                System.Diagnostics.Debug.WriteLine($"Dispatcher unhandled exception: {e.Exception}");
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in non-UI threads
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    MessageBox.Show($"A critical error occurred: {ex.Message}\n\n" +
                                  "The application will now close.",
                                  "Critical Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
            catch
            {
                // If we can't show a message box, just log the error
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            }
        }

        /// <summary>
        /// Handles application startup to minimize if needed
        /// </summary>
        private static void OnAppStartup(object sender, StartupEventArgs e)
        {
            try
            {
                // Small delay to ensure main window is created
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    
                    if (Application.Current.MainWindow != null)
                    {
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                        
                        // Hide window if minimize to tray is enabled
                        if (SettingsManager.Instance.Settings.MinimizeToTray)
                        {
                            Application.Current.MainWindow.Hide();
                        }
                    }
                };
                
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during startup: {ex.Message}");
            }
        }
    }
}
