using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Manages system tray icon and context menu
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private SettingsWindow? _settingsWindow;
        private bool _isDisposed = false;

        public event EventHandler? SettingsRequested;
        public event EventHandler? ExitRequested;

        public TrayIconManager()
        {
            InitializeTrayIcon();
        }

        /// <summary>
        /// Initializes the system tray icon and context menu
        /// </summary>
        private void InitializeTrayIcon()
        {
            try
            {
                // Create context menu
                _contextMenu = new ContextMenuStrip();
                
                // Settings menu item
                var settingsItem = new ToolStripMenuItem("Settings");
                settingsItem.Click += (s, e) => OnSettingsRequested();
                _contextMenu.Items.Add(settingsItem);

                // Separator
                _contextMenu.Items.Add(new ToolStripSeparator());

                // Background volume slider
                var volumeLabel = new ToolStripLabel("Background Volume:");
                _contextMenu.Items.Add(volumeLabel);

                var volumeTrackBar = new ToolStripTrackBar();
                volumeTrackBar.Minimum = 0;
                volumeTrackBar.Maximum = 100;
                volumeTrackBar.Value = (int)(SettingsManager.Instance.Settings.BackgroundVolume * 100);
                volumeTrackBar.ValueChanged += (s, e) =>
                {
                    var volume = volumeTrackBar.Value / 100.0f;
                    SettingsManager.Instance.UpdateBackgroundVolume(volume);
                    // Notify audio manager of volume change
                    OnVolumeChanged(volume);
                };
                _contextMenu.Items.Add(volumeTrackBar);

                // Separator
                _contextMenu.Items.Add(new ToolStripSeparator());

                // Autostart toggle
                var autostartItem = new ToolStripMenuItem("Start with Windows");
                autostartItem.Checked = SettingsManager.Instance.Settings.Autostart;
                autostartItem.Click += (s, e) =>
                {
                    autostartItem.Checked = !autostartItem.Checked;
                    SettingsManager.Instance.UpdateAutostart(autostartItem.Checked);
                };
                _contextMenu.Items.Add(autostartItem);

                // Separator
                _contextMenu.Items.Add(new ToolStripSeparator());

                // Exit menu item
                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) => OnExitRequested();
                _contextMenu.Items.Add(exitItem);

                // Create notify icon
                _notifyIcon = new NotifyIcon
                {
                    Icon = GetApplicationIcon(),
                    Text = "Smart Audio Auto-Leveler",
                    ContextMenuStrip = _contextMenu,
                    Visible = true
                };

                // Double-click to open settings
                _notifyIcon.DoubleClick += (s, e) => OnSettingsRequested();

                // Show balloon tip on first run
                ShowWelcomeBalloon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing tray icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the application icon for the tray
        /// </summary>
        private Icon GetApplicationIcon()
        {
            try
            {
                // Try to load from embedded resources first
                var assembly = Assembly.GetExecutingAssembly();
                var iconStream = assembly.GetManifestResourceStream("SmartAudioAutoLeveler.Assets.app-icon.ico");
                
                if (iconStream != null)
                {
                    return new Icon(iconStream);
                }

                // Fallback to default system icon
                return SystemIcons.Application;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        /// <summary>
        /// Shows welcome balloon tip
        /// </summary>
        private void ShowWelcomeBalloon()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(
                        3000, // 3 seconds
                        "Smart Audio Auto-Leveler",
                        "Running in system tray. Double-click to open settings.",
                        ToolTipIcon.Info
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing balloon tip: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a notification balloon
        /// </summary>
        public void ShowNotification(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(3000, title, text, icon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the settings window
        /// </summary>
        public void OpenSettings()
        {
            try
            {
                if (_settingsWindow == null || !_settingsWindow.IsVisible)
                {
                    _settingsWindow = new SettingsWindow();
                    _settingsWindow.Closed += (s, e) => _settingsWindow = null;
                    _settingsWindow.Show();
                    _settingsWindow.Activate();
                }
                else
                {
                    _settingsWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the tray icon tooltip
        /// </summary>
        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = text;
            }
        }

        /// <summary>
        /// Hides the tray icon
        /// </summary>
        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        /// <summary>
        /// Shows the tray icon
        /// </summary>
        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        private void OnSettingsRequested()
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnVolumeChanged(float volume)
        {
            // This will be handled by the main application
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                _settingsWindow?.Close();
                _settingsWindow = null;

                _notifyIcon?.Dispose();
                _notifyIcon = null;

                _contextMenu?.Dispose();
                _contextMenu = null;
            }
        }
    }

    /// <summary>
    /// Custom track bar for the context menu
    /// </summary>
    public class ToolStripTrackBar : ToolStripControlHost
    {
        public ToolStripTrackBar() : base(new TrackBar())
        {
            TrackBar.Minimum = 0;
            TrackBar.Maximum = 100;
            TrackBar.TickFrequency = 10;
            TrackBar.TickStyle = TickStyle.BottomRight;
            TrackBar.Width = 100;
        }

        public TrackBar TrackBar => Control as TrackBar ?? throw new InvalidOperationException("Control is not a TrackBar");

        public int Value
        {
            get => TrackBar.Value;
            set => TrackBar.Value = value;
        }

        public int Minimum
        {
            get => TrackBar.Minimum;
            set => TrackBar.Minimum = value;
        }

        public int Maximum
        {
            get => TrackBar.Maximum;
            set => TrackBar.Maximum = value;
        }

        public event EventHandler? ValueChanged
        {
            add => TrackBar.ValueChanged += value;
            remove => TrackBar.ValueChanged -= value;
        }
    }
}
