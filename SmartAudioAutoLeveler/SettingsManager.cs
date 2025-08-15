using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Manages application settings persistence and autostart configuration
    /// </summary>
    public class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartAudioAutoLeveler"
        );
        
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
        
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static SettingsManager? _instance;
        private static readonly object _lockObject = new object();

        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        _instance ??= new SettingsManager();
                    }
                }
                return _instance;
            }
        }

        public AppSettings Settings { get; private set; }

        private SettingsManager()
        {
            Settings = LoadSettings();
        }

        /// <summary>
        /// Loads settings from file or creates default settings
        /// </summary>
        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }

            // Return default settings if loading fails
            return new AppSettings();
        }

        /// <summary>
        /// Saves current settings to file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(SettingsDirectory);

                var json = JsonSerializer.Serialize(Settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates background volume setting
        /// </summary>
        public void UpdateBackgroundVolume(float volume)
        {
            Settings.BackgroundVolume = Math.Clamp(volume, 0.0f, 1.0f);
            SaveSettings();
        }

        /// <summary>
        /// Adds an app to the exclusion list
        /// </summary>
        public void AddExcludedApp(string appName)
        {
            if (!string.IsNullOrWhiteSpace(appName) && !Settings.ExcludedApps.Contains(appName))
            {
                Settings.ExcludedApps.Add(appName);
                SaveSettings();
            }
        }

        /// <summary>
        /// Removes an app from the exclusion list
        /// </summary>
        public void RemoveExcludedApp(string appName)
        {
            if (Settings.ExcludedApps.Remove(appName))
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// Updates autostart setting
        /// </summary>
        public void UpdateAutostart(bool enabled)
        {
            Settings.Autostart = enabled;
            SaveSettings();
            SetAutostart(enabled);
        }

        /// <summary>
        /// Sets or removes autostart registry entry
        /// </summary>
        private void SetAutostart(bool enabled)
        {
            try
            {
                const string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                const string valueName = "SmartAudioAutoLeveler";

                using var key = Registry.CurrentUser.OpenSubKey(keyName, true);
                if (key != null)
                {
                    if (enabled)
                    {
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key.SetValue(valueName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(valueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting autostart: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if autostart is currently enabled in registry
        /// </summary>
        public bool IsAutostartEnabled()
        {
            try
            {
                const string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                const string valueName = "SmartAudioAutoLeveler";

                using var key = Registry.CurrentUser.OpenSubKey(keyName);
                if (key != null)
                {
                    var value = key.GetValue(valueName);
                    return value != null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking autostart: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            Settings = new AppSettings();
            SaveSettings();
            SetAutostart(false);
        }

        /// <summary>
        /// Gets the settings file path for debugging
        /// </summary>
        public string GetSettingsFilePath()
        {
            return SettingsFilePath;
        }
    }

    /// <summary>
    /// Application settings model
    /// </summary>
    public class AppSettings
    {
        [JsonPropertyName("backgroundVolume")]
        public float BackgroundVolume { get; set; } = 0.5f;

        [JsonPropertyName("excludedApps")]
        public List<string> ExcludedApps { get; set; } = new List<string>();

        [JsonPropertyName("autostart")]
        public bool Autostart { get; set; } = false;

        [JsonPropertyName("minimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;

        [JsonPropertyName("startMinimized")]
        public bool StartMinimized { get; set; } = true;

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "System"; // System, Light, Dark

        [JsonPropertyName("updateInterval")]
        public int UpdateInterval { get; set; } = 100; // milliseconds

        [JsonPropertyName("enableNotifications")]
        public bool EnableNotifications { get; set; } = true;

        [JsonPropertyName("lastVersion")]
        public string LastVersion { get; set; } = "1.0.0";

        [JsonPropertyName("lastRun")]
        public DateTime LastRun { get; set; } = DateTime.Now;
    }
}
