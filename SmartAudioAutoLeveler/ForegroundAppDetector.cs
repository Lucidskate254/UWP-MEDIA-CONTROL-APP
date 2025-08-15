using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAudioAutoLeveler
{
    /// <summary>
    /// Detects foreground application changes using Win32 hooks and events
    /// </summary>
    public class ForegroundAppDetector : IDisposable
    {
        // Win32 API declarations
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Win32 constants
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        // Delegate for WinEvent hook
        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, long idObject, long idChild, uint dwEventThread, uint dwmsEventTime);

        private readonly WinEventProc _winEventProc;
        private IntPtr _winEventHook = IntPtr.Zero;
        private bool _isDisposed = false;
        private int _currentForegroundProcessId = -1;

        public event EventHandler<ForegroundAppChangedEventArgs>? ForegroundAppChanged;

        public int CurrentForegroundProcessId => _currentForegroundProcessId;

        public ForegroundAppDetector()
        {
            _winEventProc = OnForegroundWindowChanged;
            StartMonitoring();
        }

        /// <summary>
        /// Starts monitoring for foreground window changes
        /// </summary>
        private void StartMonitoring()
        {
            try
            {
                // Get initial foreground process
                UpdateForegroundProcess();

                // Set up WinEvent hook for foreground changes
                _winEventHook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_FOREGROUND,
                    GetModuleHandle("user32.dll"),
                    _winEventProc,
                    0, // Monitor all processes
                    0, // Monitor all threads
                    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
                );

                if (_winEventHook == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to set WinEvent hook");
                }
            }
            catch (Exception ex)
            {
                // Fallback to polling if hook fails
                StartPollingFallback();
            }
        }

        /// <summary>
        /// Fallback method using polling if WinEvent hook fails
        /// </summary>
        private void StartPollingFallback()
        {
            Task.Run(async () =>
            {
                while (!_isDisposed)
                {
                    try
                    {
                        UpdateForegroundProcess();
                        await Task.Delay(100); // Poll every 100ms
                    }
                    catch
                    {
                        await Task.Delay(1000); // Slower polling on error
                    }
                }
            });
        }

        /// <summary>
        /// Updates the current foreground process ID
        /// </summary>
        private void UpdateForegroundProcess()
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero)
                {
                    uint processId;
                    if (GetWindowThreadProcessId(foregroundWindow, out processId))
                    {
                        var newProcessId = (int)processId;
                        if (newProcessId != _currentForegroundProcessId)
                        {
                            var oldProcessId = _currentForegroundProcessId;
                            _currentForegroundProcessId = newProcessId;

                            // Raise event on UI thread
                            Task.Run(() =>
                            {
                                ForegroundAppChanged?.Invoke(this, new ForegroundAppChangedEventArgs(oldProcessId, newProcessId));
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue monitoring
                System.Diagnostics.Debug.WriteLine($"Error updating foreground process: {ex.Message}");
            }
        }

        /// <summary>
        /// WinEvent callback for foreground window changes
        /// </summary>
        private void OnForegroundWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, long idObject, long idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                // Small delay to ensure window is fully activated
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    UpdateForegroundProcess();
                });
            }
        }

        /// <summary>
        /// Gets the current foreground process ID synchronously
        /// </summary>
        public int GetCurrentForegroundProcessId()
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero)
                {
                    uint processId;
                    if (GetWindowThreadProcessId(foregroundWindow, out processId))
                    {
                        return (int)processId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting foreground process ID: {ex.Message}");
            }
            return -1;
        }

        /// <summary>
        /// Manually refresh the foreground process detection
        /// </summary>
        public void Refresh()
        {
            UpdateForegroundProcess();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_winEventHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_winEventHook);
                    _winEventHook = IntPtr.Zero;
                }
            }
        }
    }

    public class ForegroundAppChangedEventArgs : EventArgs
    {
        public int PreviousProcessId { get; }
        public int NewProcessId { get; }

        public ForegroundAppChangedEventArgs(int previousProcessId, int newProcessId)
        {
            PreviousProcessId = previousProcessId;
            NewProcessId = newProcessId;
        }
    }
}
