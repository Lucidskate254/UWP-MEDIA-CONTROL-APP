using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AudioManager _audioManager;
        private readonly ForegroundAppDetector _foregroundDetector;
        private readonly TrayIconManager _trayIconManager;
        private readonly DispatcherTimer _statusUpdateTimer;
        private readonly List<string> _recentActivity;
        private bool _isClosing = false;

        public AudioManager AudioManager => _audioManager;

        public MainWindow()
        {
            InitializeComponent();
            
            _recentActivity = new List<string>();
            
            // Initialize core components
            try
            {
                _audioManager = new AudioManager();
                _foregroundDetector = new ForegroundAppDetector();
                _trayIconManager = new TrayIconManager();
                
                // Subscribe to events
                _audioManager.SessionAdded += OnAudioSessionAdded;
                _audioManager.SessionRemoved += OnAudioSessionRemoved;
                _audioManager.VolumeChanged += OnVolumeChanged;
                _foregroundDetector.ForegroundAppChanged += OnForegroundAppChanged;
                _trayIconManager.SettingsRequested += OnSettingsRequested;
                _trayIconManager.ExitRequested += OnExitRequested;
                
                // Set initial background volume from settings
                _audioManager.BackgroundVolume = SettingsManager.Instance.Settings.BackgroundVolume;
                
                // Set up status update timer
                _statusUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                _statusUpdateTimer.Tick += OnStatusUpdateTimer;
                _statusUpdateTimer.Start();
                
                // Initial status update
                UpdateStatus();
                
                AddRecentActivity("Application started successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AddRecentActivity($"Initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the status display
        /// </summary>
        private void UpdateStatus()
        {
            try
            {
                // Update audio manager status
                if (_audioManager != null)
                {
                    AudioManagerStatusIndicator.Fill = Brushes.Green;
                    AudioManagerStatusText.Text = "Audio Manager: Running";
                    
                    var sessions = _audioManager.GetActiveSessions();
                    ActiveSessionsStatusText.Text = $"Active Audio Sessions: {sessions.Count}";
                }
                else
                {
                    AudioManagerStatusIndicator.Fill = Brushes.Red;
                    AudioManagerStatusText.Text = "Audio Manager: Error";
                    ActiveSessionsStatusText.Text = "Active Audio Sessions: Error";
                }

                // Update foreground detection status
                if (_foregroundDetector != null)
                {
                    ForegroundDetectionStatusIndicator.Fill = Brushes.Green;
                    ForegroundDetectionStatusText.Text = "Foreground Detection: Running";
                    
                    var currentProcessId = _foregroundDetector.CurrentForegroundProcessId;
                    if (currentProcessId != -1)
                    {
                        var processName = GetProcessName(currentProcessId);
                        ForegroundAppStatusIndicator.Fill = Brushes.Green;
                        ForegroundAppStatusText.Text = $"Foreground App: {processName} (PID: {currentProcessId})";
                    }
                    else
                    {
                        ForegroundAppStatusIndicator.Fill = Brushes.Gray;
                        ForegroundAppStatusText.Text = "Foreground App: None";
                    }
                }
                else
                {
                    ForegroundDetectionStatusIndicator.Fill = Brushes.Red;
                    ForegroundDetectionStatusText.Text = "Foreground Detection: Error";
                    ForegroundAppStatusIndicator.Fill = Brushes.Red;
                    ForegroundAppStatusText.Text = "Foreground App: Error";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the process name from process ID
        /// </summary>
        private string GetProcessName(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return $"Unknown ({processId})";
            }
        }

        /// <summary>
        /// Adds activity to the recent activity log
        /// </summary>
        private void AddRecentActivity(string activity)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var entry = $"[{timestamp}] {activity}";
                
                _recentActivity.Insert(0, entry);
                
                // Keep only last 10 entries
                if (_recentActivity.Count > 10)
                {
                    _recentActivity.RemoveAt(_recentActivity.Count - 1);
                }
                
                // Update UI
                RecentActivityText.Text = string.Join("\n", _recentActivity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding recent activity: {ex.Message}");
            }
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _trayIconManager.OpenSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
            AddRecentActivity("Status refreshed manually");
        }

        private void MinimizeToTrayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                AddRecentActivity("Application minimized to system tray");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error minimizing to tray: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnStatusUpdateTimer(object? sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void OnAudioSessionAdded(object? sender, AudioSessionEventArgs e)
        {
            try
            {
                var processName = GetProcessName(e.ProcessId);
                AddRecentActivity($"Audio session added: {processName} (PID: {e.ProcessId})");
                
                // Update foreground process if this is the first session
                if (_foregroundDetector.CurrentForegroundProcessId == -1)
                {
                    _foregroundDetector.Refresh();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling session added: {ex.Message}");
            }
        }

        private void OnAudioSessionRemoved(object? sender, AudioSessionEventArgs e)
        {
            try
            {
                var processName = GetProcessName(e.ProcessId);
                AddRecentActivity($"Audio session removed: {processName} (PID: {e.ProcessId})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling session removed: {ex.Message}");
            }
        }

        private void OnVolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            try
            {
                var processName = GetProcessName(e.ProcessId);
                var volumePercent = (e.NewVolume * 100).ToString("F0");
                AddRecentActivity($"Volume changed for {processName}: {volumePercent}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling volume change: {ex.Message}");
            }
        }

        private void OnForegroundAppChanged(object? sender, ForegroundAppChangedEventArgs e)
        {
            try
            {
                if (e.NewProcessId != -1)
                {
                    var processName = GetProcessName(e.NewProcessId);
                    AddRecentActivity($"Foreground app changed to: {processName} (PID: {e.NewProcessId})");
                    
                    // Update audio manager with new foreground process
                    if (_audioManager != null)
                    {
                        _audioManager.ForegroundProcessId = e.NewProcessId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling foreground app change: {ex.Message}");
            }
        }

        private void OnSettingsRequested(object? sender, EventArgs e)
        {
            try
            {
                Show();
                Activate();
                AddRecentActivity("Settings opened from system tray");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling settings request: {ex.Message}");
            }
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            Close();
        }

        #endregion

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            // Hide window when minimized if setting is enabled
            if (WindowState == WindowState.Minimized && 
                SettingsManager.Instance.Settings.MinimizeToTray)
            {
                Hide();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;
                
                try
                {
                    // Stop timer
                    _statusUpdateTimer?.Stop();
                    
                    // Dispose components
                    _audioManager?.Dispose();
                    _foregroundDetector?.Dispose();
                    _trayIconManager?.Dispose();
                    
                    // Update settings
                    SettingsManager.Instance.Settings.LastRun = DateTime.Now;
                    SettingsManager.Instance.SaveSettings();
                    
                    AddRecentActivity("Application shutting down");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
                }
            }
            
            base.OnClosing(e);
        }
    }
}
