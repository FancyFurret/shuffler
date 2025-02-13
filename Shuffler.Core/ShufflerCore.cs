using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shuffler.Core.Models;
using Shuffler.Core.Services;

namespace Shuffler.Core;

public enum ShufflerState
{
    Stopped,
    Started,
    Paused
}

public class ShufflerCore : IDisposable
{
    private readonly Observable<ShufflerSession> _session = new(new ShufflerSession());
    public IObservable<ShufflerSession> Session => _session;

    private readonly Observable<ShufflerState> _state = new(ShufflerState.Stopped);
    public IObservable<ShufflerState> State => _state;
    public bool IsRunning => _state.Value != ShufflerState.Stopped;

    private readonly Observable<bool> _isLoading = new(true);
    public IObservable<bool> IsLoading => _isLoading;

    private readonly ShufflerConfig _config;
    private readonly GamepadManager _gamepadManager;

    private Queue<SessionPlayer> _playerQueue = new();
    private readonly ProcessMonitor _processMonitor = new();
    private readonly ILogger _logger = LogManager.Create(nameof(ShufflerCore));

    private CancellationTokenSource? _shuffleCts;
    private Task? _shuffleTask;
    private readonly CancellationTokenSource _initCts = new();

    public ShufflerCore(ShufflerConfig config, GamepadManager gamepadManager)
    {
        _config = config;
        _gamepadManager = gamepadManager;
    }

    public IEnumerable<ShufflerController> GetAvailableControllers() =>
        _gamepadManager.GetAvailableControllers(_session.Value.Players);

    public bool AssignController(SessionPlayer player, ShufflerController controller)
    {
        var result = _gamepadManager.AssignController(player, controller, _session.Value.Players);
        if (result)
            _session.Changed();
        return result;
    }

    public void UnassignController(SessionPlayer player)
    {
        _gamepadManager.UnassignController(player);
        _session.Changed();
    }

    public Task<ControllerPressResult?> WaitForControllerInput(CancellationToken cancellationToken)
    {
        return _gamepadManager.WaitForButtonPress(cancellationToken);
    }

    public void TestVibration(SessionPlayer player)
    {
        if (player.AssignedController == null)
            return;
        _gamepadManager.Vibrate(player.AssignedController, 1.0f, 500);
    }

    public Task TestSuccessVibration(SessionPlayer player)
    {
        if (player.AssignedController == null)
            return Task.CompletedTask;
        return _gamepadManager.VibrateSuccess(player.AssignedController);
    }

    public async Task<ShufflerSession> LoadPreset(ShufflerPreset preset)
    {
        if (_state.Value != ShufflerState.Stopped)
            throw new InvalidOperationException("Cannot load preset while shuffler is running");

        _isLoading.Value = true;
        try
        {
            var previousSession = _session.Value;
            var session = new ShufflerSession
            {
                Preset = preset,
                Players = previousSession?.Players ?? [],
                Games = preset.Games.Select(game => new SessionGame
                {
                    GameConfig = game.GameConfig,
                    Process = new GameProcess(game.GameConfig, _processMonitor),
                }).ToList()
            };

            // Move CPU-bound process scanning to a background thread
            var processes = await Task.Run(() =>
            {
                var processDict = new Dictionary<string, List<Process>>();
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.MainModule == null) continue;
                        var key = process.MainModule.FileName;
                        if (!processDict.ContainsKey(key))
                            processDict[key] = [];
                        processDict[key].Add(process);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                return processDict;
            });

            foreach (var game in session.Games)
                if (processes.TryGetValue(game.Process.Config.ExePath, out var process))
                    await game.Process.ConnectToExistingProcess(process.First(), _initCts.Token);

            _session.Value = session;
            return session;
        }
        finally
        {
            _isLoading.Value = false;
        }
    }

    public void AddPlayer(PlayerConfig player)
    {
        _session.Value.Players.Add(new SessionPlayer
        {
            Config = player
        });
        _session.Changed();
    }

    public void RemovePlayer(SessionPlayer player)
    {
        if (player.AssignedController != null)
            UnassignController(player);

        _session.Value.Players.Remove(player);
        _session.Changed();
        Reset();
    }

    public void StartAsync()
    {
        if (IsRunning)
            return;

        _shuffleCts = new CancellationTokenSource();
        _shuffleTask = Task.Run(() => RunShuffleLoop(_shuffleCts.Token), _shuffleCts.Token);
        _state.Value = ShufflerState.Started;
    }

    public void Pause()
    {
        if (_state.Value == ShufflerState.Started)
            _state.Value = ShufflerState.Paused;
        else if (_state.Value == ShufflerState.Paused)
            _state.Value = ShufflerState.Started;
    }

    public async Task StopAsync()
    {
        if (_shuffleCts != null)
        {
            await _shuffleCts.CancelAsync();
            _shuffleCts = null;
        }

        if (_shuffleTask != null)
        {
            await _shuffleTask;
            _shuffleTask = null;
        }

        Reset();
        _state.Value = ShufflerState.Stopped;
    }

    private async Task RunShuffleLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_state.Value == ShufflerState.Started)
                    Shuffle();

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void RefillPlayerQueue()
    {
        // var shuffledPlayers = _players.OrderBy(x => Random.Shared.Next()).ToList();
        // _playerQueue = new Queue<Player>(shuffledPlayers);
    }

    public void Shuffle()
    {
        var random = new Random();
        // var process = _shufflerGames[random.Next(_shufflerGames.Count)];

        // _logger.Info($"Shuffling to {process.Config.Name}");

        // Get next player from queue, refill if empty
        if (!_playerQueue.Any())
        {
            RefillPlayerQueue();
        }

        var nextPlayer = _playerQueue.Dequeue();
        // _currentPlayerIndex = _players.IndexOf(nextPlayer);
        if (_gamepadManager != null)
        {
            // TODO: Set active controller
            // _gamepadManager.SetActiveController(nextPlayer.ControllerIndex);
        }

        // _logger.Info($"{nextPlayer.Name}'s turn");

        // SwitchToProcess(process);
    }

    private void SwitchToProcess(GameProcess process)
    {
        // foreach (var p in _shufflerGames)
        // {
        // if (p == process) continue;
        // if (p.Suspended) continue;
        // _logger.Debug($"Suspending {p.Config.Name}");
        // p.Hide();
        // p.Mute();
        // p.Suspend();
        // }

        process.Resume();
        process.Show();
        process.Unmute();
    }

    public void Reset()
    {
        _playerQueue.Clear();
        RefillPlayerQueue();
    }

    public void TogglePlayerActive(SessionPlayer player)
    {
        player.IsActive = !player.IsActive;
        _session.Changed();
        Reset();
    }

    public void Dispose()
    {
        _initCts.Cancel();
        _initCts.Dispose();
        _shuffleCts?.Cancel();
        _shuffleCts?.Dispose();
        _gamepadManager?.Dispose();
    }
}