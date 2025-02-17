using System.Diagnostics;
using Shuffler.Core.Hook;
using Shuffler.Core.Services;
using Shuffler.Core.Utils;

namespace Shuffler.Core;

public enum GameState
{
    Stopped,
    Loading,
    Running,
    Paused
}

public class GameProcess : IAsyncDisposable
{
    public readonly GameConfig Config;

    private readonly Observable<GameState> _state = new(GameState.Stopped);
    public IObservable<GameState> State => _state;
    public bool Suspended => _state.Value == GameState.Paused;
    public bool Running => _state.Value == GameState.Running;

    private readonly Observable<bool> _muted = new(false);
    public IObservable<bool> Muted => _muted;

    public Process? Process => _hook?.Process;

    private AudioManager? _audioManager;
    private ShufflerHook? _hook;
    private IDisposable? _monitor;
    private Process? _currentProcess;

    private static readonly ILogger Logger = LogManager.Create(nameof(GameProcess));

    private TaskCompletionSource<bool>? _processStartTcs;
    private TaskCompletionSource<bool>? _processStopTcs;

    public GameProcess(GameConfig config, ProcessMonitor processMonitor, bool loading)
    {
        Config = config;

        // Start monitoring for this game's process
        _monitor = processMonitor.Monitor(
            Config.ExePath,
            OnProcessCreated,
            OnProcessTerminated
        );
        
        _state.Value = loading ? GameState.Loading : GameState.Stopped;
    }
    
    private void OnProcessCreated(int processId)
    {
        Logger.Info($"Process created: {processId}");
        Task.Run(() => Connect(processId));
        _processStartTcs?.TrySetResult(true);
    }

    private void OnProcessTerminated(int processId)
    {
        Logger.Info($"Process terminated: {processId}");
        if (_currentProcess?.Id == processId)
            _state.Value = GameState.Stopped;
    }

    public async Task ConnectToExistingProcess(Process? process, CancellationToken cancellationToken = default)
    {
        if (process == null)
        {
            _state.Value = GameState.Stopped;
            return;
        }
        
        var wasSuspended = ProcessManager.IsSuspended(process);
        if (wasSuspended) ProcessManager.ResumeProcess(process.Handle);
        await Connect(process.Id, true);
        if (wasSuspended) ProcessManager.SuspendProcess(process.Handle);

        _state.Value = wasSuspended ? GameState.Paused : GameState.Running;
        _muted.Value = _audioManager!.GetApplicationMute();
    }


    private async Task Connect(int pid, bool existing = false)
    {
        if (_state.Value != GameState.Stopped && _state.Value != GameState.Loading)
        {
            Logger.Warning("Ignoring externally launched process because game is already running");
            return;
        }

        // Hook the process
        _currentProcess = Process.GetProcessById(pid);
        if (!existing) ProcessManager.SuspendProcess(_currentProcess.Handle);
        _hook = await ShufflerHook.HookProcessAsync(_currentProcess);
        _audioManager = new AudioManager(_hook.Process.Id);
        if (!existing) ProcessManager.ResumeProcess(_currentProcess.Handle);
        if (!existing) _state.Value = GameState.Running;
    }

    private async Task WaitForLaunch(int timeoutSeconds)
    {
        try
        {
            _processStartTcs = new TaskCompletionSource<bool>();
            await TaskUtils.WaitForEvent(_processStartTcs, "Process start", timeoutSeconds);
        }
        catch
        {
            _state.Value = GameState.Stopped;
            throw;
        }
        finally
        {
            _processStartTcs = null;
        }
    }

    private async Task WaitForStop(int timeoutSeconds)
    {
        try
        {
            _processStopTcs = new TaskCompletionSource<bool>();
            await TaskUtils.WaitForEvent(_processStopTcs, "Process stop", timeoutSeconds);
        }
        finally
        {
            _processStopTcs = null;
        }
    }

    public async Task StartAsync()
    {
        if (Running) return;

        _state.Value = GameState.Loading;
        try
        {
            if (Config.SteamAppId.HasValue)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{Config.SteamAppId}",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                await WaitForLaunch(15);
            }
            else if (!string.IsNullOrEmpty(Config.ExePath))
            {
                ProcessManager.CreateProcess(Config.ExePath);
                await WaitForLaunch(15);
            }
            else
                throw new InvalidOperationException("Game configuration is invalid");
        }
        catch
        {
            _state.Value = GameState.Stopped;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_state.Value == GameState.Paused)
            Resume();

        EnsureRunning();

        _state.Value = GameState.Loading;
        try
        {
            if (_currentProcess != null)
            {
                WindowManager.Close(_currentProcess.MainWindowHandle);
                await WaitForStop(15);
            }

            if (_hook != null)
            {
                await _hook.DisposeAsync();
                _hook = null;
            }

            _audioManager = null;
            _state.Value = GameState.Stopped;
        }
        catch
        {
            _state.Value = GameState.Stopped;
            throw;
        }
    }

    private void EnsureRunning()
    {
        if (!Running)
            throw new InvalidOperationException("Game is not running");
    }

    public void Mute()
    {
        _audioManager!.SetApplicationMute(true);
        _muted.Value = true;
    }

    public void Unmute()
    {
        _audioManager!.SetApplicationMute(false);
        _muted.Value = false;
    }

    public void ToggleMute()
    {
        if (_muted.Value)
            Unmute();
        else
            Mute();
    }

    public void Hide()
    {
        EnsureRunning();
        WindowManager.Minimize(Process!.MainWindowHandle);
    }

    public void Show()
    {
        EnsureRunning();
        WindowManager.BringToFront(Process!.MainWindowHandle);
        WindowManager.Restore(Process!.MainWindowHandle);
        WindowManager.Show(Process!.MainWindowHandle);
    }

    public void Suspend()
    {
        EnsureRunning();
        ProcessManager.SuspendProcess(Process!.Handle);
        _state.Value = GameState.Paused;
    }

    public void Resume()
    {
        if (!Suspended)
            throw new InvalidOperationException("Game is not suspended");
        ProcessManager.ResumeProcess(Process!.Handle);
        _state.Value = GameState.Running;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hook != null)
        {
            await _hook.DisposeAsync();
            _hook = null;
        }

        _monitor?.Dispose();
        _monitor = null;

        if (_monitor != null)
        {
            _monitor.Dispose();
            _monitor = null;
        }

        _audioManager = null;
    }
}