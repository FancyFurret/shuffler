using System.Management;

namespace Shuffler.Core;

public class ProcessMonitor : IDisposable
{
    private readonly ManagementEventWatcher _startWatcher;
    private readonly ManagementEventWatcher _stopWatcher;
    private static readonly ILogger Logger = LogManager.Create(nameof(ProcessMonitor));
    private bool _isDisposed;

    private readonly Dictionary<string, List<(Action<int> onCreate, Action<int> onTerminate)>> _monitors =
        new(StringComparer.OrdinalIgnoreCase);

    public ProcessMonitor()
    {
        // WMI query for process creation
        var startQuery = new WqlEventQuery(
            "SELECT * FROM Win32_ProcessStartTrace"
        );

        // WMI query for process termination
        var stopQuery = new WqlEventQuery(
            "SELECT * FROM Win32_ProcessStopTrace"
        );

        _startWatcher = new ManagementEventWatcher(startQuery);
        _stopWatcher = new ManagementEventWatcher(stopQuery);

        _startWatcher.EventArrived += (sender, args) =>
        {
            try
            {
                var processId = int.Parse(args.NewEvent.Properties["ProcessID"].Value.ToString()!);
                var processName = args.NewEvent.Properties["ProcessName"].Value.ToString()!;
                var processNameWithoutExt = Path.GetFileNameWithoutExtension(processName);

                if (!_monitors.TryGetValue(processNameWithoutExt, out var handlers)) return;

                foreach (var (onCreate, _) in handlers)
                {
                    try
                    {
                        onCreate(processId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error in process creation handler: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling process start event: {ex.Message}");
            }
        };

        _stopWatcher.EventArrived += (sender, args) =>
        {
            try
            {
                var processId = int.Parse(args.NewEvent.Properties["ProcessID"].Value.ToString()!);
                var processName = args.NewEvent.Properties["ProcessName"].Value.ToString()!;
                var processNameWithoutExt = Path.GetFileNameWithoutExtension(processName);

                if (!_monitors.TryGetValue(processNameWithoutExt, out var handlers)) return;

                foreach (var (_, onTerminate) in handlers)
                {
                    try
                    {
                        onTerminate(processId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error in process termination handler: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling process stop event: {ex.Message}");
            }
        };

        Start();
    }

    public IDisposable Monitor(string exePath, Action<int> onCreate, Action<int> onTerminate)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ProcessMonitor));

        var processNameWithoutExt = Path.GetFileNameWithoutExtension(exePath);
        if (!_monitors.ContainsKey(processNameWithoutExt))
            _monitors[processNameWithoutExt] = new List<(Action<int>, Action<int>)>();

        var handlers = (onCreate, onTerminate);
        _monitors[processNameWithoutExt].Add(handlers);

        // Return a disposable that will remove these handlers
        return new ProcessMonitorSubscription(this, processNameWithoutExt, handlers);
    }

    private void RemoveMonitor(string processNameWithoutExt, (Action<int> onCreate, Action<int> onTerminate) handlers)
    {
        if (!_monitors.TryGetValue(processNameWithoutExt, out var list)) return;

        list.Remove(handlers);
        if (list.Count == 0)
            _monitors.Remove(processNameWithoutExt);
    }

    private void Start()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ProcessMonitor));

        _startWatcher.Start();
        _stopWatcher.Start();
        Logger.Info("Process monitor started");
    }

    private void Stop()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ProcessMonitor));

        _startWatcher.Stop();
        _stopWatcher.Stop();
        Logger.Info("Process monitor stopped");
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        Stop();
        _startWatcher.Dispose();
        _stopWatcher.Dispose();
        _monitors.Clear();
        _isDisposed = true;
    }

    private class ProcessMonitorSubscription(
        ProcessMonitor monitor,
        string processNameWithoutExt,
        (Action<int> onCreate, Action<int> onTerminate) handlers)
        : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            monitor.RemoveMonitor(processNameWithoutExt, handlers);
            _isDisposed = true;
        }
    }
}