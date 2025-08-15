using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Threading.Tasks; // Added missing import for Task

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Manages audio sessions and volume control using Windows CoreAudio APIs
    /// </summary>
    public class AudioManager : IDisposable
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly MMDevice _defaultDevice;
        private readonly AudioSessionManager2 _sessionManager;
        private readonly Dictionary<int, float> _originalVolumes;
        private readonly Dictionary<int, SimpleAudioVolume> _sessionVolumes;
        private readonly Dictionary<int, AudioSessionControl2> _activeSessions;
        private readonly object _lockObject = new object();
        
        private float _backgroundVolume = 0.5f;
        private int _foregroundProcessId = -1;
        private bool _isDisposed = false;

        public event EventHandler<AudioSessionEventArgs>? SessionAdded;
        public event EventHandler<AudioSessionEventArgs>? SessionRemoved;
        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        public float BackgroundVolume
        {
            get => _backgroundVolume;
            set
            {
                if (_backgroundVolume != value)
                {
                    _backgroundVolume = Math.Clamp(value, 0.0f, 1.0f);
                    AdjustAllVolumes();
                }
            }
        }

        public int ForegroundProcessId
        {
            get => _foregroundProcessId;
            set
            {
                if (_foregroundProcessId != value)
                {
                    _foregroundProcessId = value;
                    AdjustAllVolumes();
                }
            }
        }

        public AudioManager()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _sessionManager = _defaultDevice.AudioSessionManager2;
            _originalVolumes = new Dictionary<int, float>();
            _sessionVolumes = new Dictionary<int, SimpleAudioVolume>();
            _activeSessions = new Dictionary<int, AudioSessionControl2>();

            // Subscribe to session events
            _sessionManager.OnSessionCreated += OnSessionCreated;
            
            // Initialize existing sessions
            RefreshSessions();
        }

        /// <summary>
        /// Refreshes the list of active audio sessions
        /// </summary>
        public void RefreshSessions()
        {
            lock (_lockObject)
            {
                var sessions = _sessionManager.Sessions;
                var currentSessionIds = new HashSet<int>();

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i] as AudioSessionControl2;
                    if (session?.State == AudioSessionState.AudioSessionStateActive)
                    {
                        var processId = session.ProcessID;
                        currentSessionIds.Add(processId);

                        if (!_activeSessions.ContainsKey(processId))
                        {
                            _activeSessions[processId] = session;
                            var simpleVol = session.QueryInterface<SimpleAudioVolume>();
                            _sessionVolumes[processId] = simpleVol;
                            
                            // Store original volume if not already stored
                            if (!_originalVolumes.ContainsKey(processId))
                            {
                                _originalVolumes[processId] = simpleVol.MasterVolume;
                            }

                            // Subscribe to session events
                            session.OnStateChanged += OnSessionStateChanged;
                            session.OnDisconnected += OnSessionDisconnected;

                            SessionAdded?.Invoke(this, new AudioSessionEventArgs(processId, session));
                        }
                    }
                }

                // Remove disconnected sessions
                var disconnectedSessions = _activeSessions.Keys.Except(currentSessionIds).ToList();
                foreach (var processId in disconnectedSessions)
                {
                    RemoveSession(processId);
                }
            }
        }

        /// <summary>
        /// Adjusts volumes for all active sessions based on foreground process
        /// </summary>
        public void AdjustAllVolumes()
        {
            if (_foregroundProcessId == -1) return;

            lock (_lockObject)
            {
                foreach (var kvp in _activeSessions)
                {
                    var processId = kvp.Key;
                    var session = kvp.Value;

                    if (_sessionVolumes.TryGetValue(processId, out var simpleVol))
                    {
                        float targetVolume;
                        if (processId == _foregroundProcessId)
                        {
                            // Restore original volume for foreground app
                            targetVolume = _originalVolumes.GetValueOrDefault(processId, 1.0f);
                        }
                        else
                        {
                            // Apply background volume reduction
                            targetVolume = _originalVolumes.GetValueOrDefault(processId, 1.0f) * _backgroundVolume;
                        }

                        if (Math.Abs(simpleVol.MasterVolume - targetVolume) > 0.01f)
                        {
                            simpleVol.MasterVolume = targetVolume;
                            VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(processId, targetVolume));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current volume for a specific process
        /// </summary>
        public float GetProcessVolume(int processId)
        {
            lock (_lockObject)
            {
                return _sessionVolumes.TryGetValue(processId, out var simpleVol) ? simpleVol.MasterVolume : 0.0f;
            }
        }

        /// <summary>
        /// Gets the original volume for a specific process
        /// </summary>
        public float GetOriginalVolume(int processId)
        {
            lock (_lockObject)
            {
                return _originalVolumes.GetValueOrDefault(processId, 1.0f);
            }
        }

        /// <summary>
        /// Gets all active session information
        /// </summary>
        public IReadOnlyDictionary<int, AudioSessionInfo> GetActiveSessions()
        {
            lock (_lockObject)
            {
                var result = new Dictionary<int, AudioSessionInfo>();
                foreach (var kvp in _activeSessions)
                {
                    var processId = kvp.Key;
                    var session = kvp.Value;
                    var simpleVol = _sessionVolumes[processId];
                    
                    result[processId] = new AudioSessionInfo
                    {
                        ProcessId = processId,
                        DisplayName = session.DisplayName,
                        IconPath = session.IconPath,
                        CurrentVolume = simpleVol.MasterVolume,
                        OriginalVolume = _originalVolumes.GetValueOrDefault(processId, 1.0f),
                        IsForeground = processId == _foregroundProcessId
                    };
                }
                return result;
            }
        }

        private void OnSessionCreated(object? sender, IAudioSessionControl newSession)
        {
            if (newSession is AudioSessionControl2 session2)
            {
                // Refresh sessions on next cycle to include the new session
                Task.Run(async () =>
                {
                    await Task.Delay(100); // Small delay to ensure session is fully initialized
                    RefreshSessions();
                });
            }
        }

        private void OnSessionStateChanged(object? sender, AudioSessionState newState)
        {
            if (sender is AudioSessionControl2 session)
            {
                if (newState == AudioSessionState.AudioSessionStateActive)
                {
                    RefreshSessions();
                }
                else if (newState == AudioSessionState.AudioSessionStateInactive)
                {
                    // Session became inactive, remove it
                    RemoveSession(session.ProcessID);
                }
            }
        }

        private void OnSessionDisconnected(object? sender, AudioSessionDisconnectReason disconnectReason)
        {
            if (sender is AudioSessionControl2 session)
            {
                RemoveSession(session.ProcessID);
            }
        }

        private void RemoveSession(int processId)
        {
            lock (_lockObject)
            {
                if (_activeSessions.TryGetValue(processId, out var session))
                {
                    session.OnStateChanged -= OnSessionStateChanged;
                    session.OnDisconnected -= OnSessionDisconnected;
                    _activeSessions.Remove(processId);
                    _sessionVolumes.Remove(processId);
                    // Don't remove original volume - keep it for when session returns
                    
                    SessionRemoved?.Invoke(this, new AudioSessionEventArgs(processId, session));
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                lock (_lockObject)
                {
                    foreach (var session in _activeSessions.Values)
                    {
                        try
                        {
                            session.OnStateChanged -= OnSessionStateChanged;
                            session.OnDisconnected -= OnSessionDisconnected;
                        }
                        catch { /* Ignore cleanup errors */ }
                    }

                    _activeSessions.Clear();
                    _sessionVolumes.Clear();
                }

                _sessionManager.OnSessionCreated -= OnSessionCreated;
                _sessionManager?.Dispose();
                _defaultDevice?.Dispose();
                _deviceEnumerator?.Dispose();
            }
        }
    }

    public class AudioSessionEventArgs : EventArgs
    {
        public int ProcessId { get; }
        public AudioSessionControl2 Session { get; }

        public AudioSessionEventArgs(int processId, AudioSessionControl2 session)
        {
            ProcessId = processId;
            Session = session;
        }
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public int ProcessId { get; }
        public float NewVolume { get; }

        public VolumeChangedEventArgs(int processId, float newVolume)
        {
            ProcessId = processId;
            NewVolume = newVolume;
        }
    }

    public class AudioSessionInfo
    {
        public int ProcessId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public float CurrentVolume { get; set; }
        public float OriginalVolume { get; set; }
        public bool IsForeground { get; set; }
    }
}
