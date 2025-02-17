using System.Diagnostics;
using Shuffler.Core.Models;

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
        _isLoading.Value = true;
        _ = Init();
    }

    private async Task Init()
    {
        var lastUsedPlayers = _config.LastUsedPlayerNames.Count > 0
            ? _config.Players.Where(p => _config.LastUsedPlayerNames.Contains(p.Name))
            : _config.Players.Take(4);

        foreach (var player in lastUsedPlayers)
            AddPlayer(player);

        var preset = _config.Presets.FirstOrDefault(p => p.Id == _config.LastUsedPresetId);
        if (preset != null)
            await LoadPreset(preset);
        else
            await LoadPreset(ShufflerPreset.Default());


        _state.Value = ShufflerState.Started;
        RollNextGame();
        RollNextPlayer();
        _session.Value.CurrentGame = _session.Value.NextGame;
        _session.Value.CurrentPlayer = _session.Value.NextPlayer;
        RollNextGame();
        RollNextPlayer();
        _isLoading.Value = false;
    }

    public async Task AssignController(SessionPlayer player, ShufflerController controller)
    {
        player.AssignedController = controller;
        await _gamepadManager.VibrateSuccess(player.AssignedController);
        _session.Changed();
    }

    public void UnassignController(SessionPlayer player)
    {
        player.AssignedController = null;
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

    public void AddPlayer(PlayerConfig config)
    {
        var players = _session.Value.Players.ToList();
        players.Add(new SessionPlayer { Config = config });
        UpdatePlayers(players);
    }

    public void RemovePlayer(SessionPlayer player)
    {
        if (player.AssignedController != null)
            UnassignController(player);

        var players = _session.Value.Players.ToList();
        players.Remove(player);
        UpdatePlayers(players);
    }

    public void TogglePlayerActive(SessionPlayer player)
    {
        player.IsActive = !player.IsActive;
        UpdatePlayers(_session.Value.Players.ToList());
    }

    public void SwapPlayer(SessionPlayer player, PlayerConfig newConfig)
    {
        player.Config = newConfig;
        UpdatePlayers(_session.Value.Players.ToList());
    }

    private void UpdatePlayers(List<SessionPlayer> players)
    {
        var addedPlayers = players.Where(p => !_session.Value.Players.Contains(p)).ToList();

        // Update the players list
        _session.Value.Players = players;

        // Reset play time for all players
        foreach (var player in _session.Value.Players)
            player.TotalPlayTime = TimeSpan.Zero;

        if (_session.Value.Preset?.PlayerSwitchMode == SwitchMode.Bagged && _session.Value.BaggedPlayers != null)
        {
            // Ensure bag only contains players that are in the session
            _session.Value.BaggedPlayers = new Queue<SessionPlayer>(
                _session.Value.BaggedPlayers.Where(p => _session.Value.Players.Contains(p))
            );

            // Add new players to bag
            foreach (var player in addedPlayers)
            {
                var list = _session.Value.BaggedPlayers.ToList();
                list.Add(player);
                _session.Value.BaggedPlayers = new Queue<SessionPlayer>(list);
            }
        }

        // If the next player was removed, choose a new one.
        if (_session.Value.NextPlayer != null && !_session.Value.Players.Contains(_session.Value.NextPlayer))
            RollNextPlayer();

        _session.Changed();
    }

    public async Task<ShufflerSession> UpdatePreset(ShufflerPreset preset)
    {
        if (_session.Value.Preset?.Id != preset.Id)
            throw new InvalidOperationException("Cannot update preset, current preset id does not match");

        var currentPreset = _session.Value.Preset!;
        currentPreset.Name = preset.Name;
        currentPreset.MinShuffleTime = preset.MinShuffleTime;
        currentPreset.MaxShuffleTime = preset.MaxShuffleTime;
        currentPreset.Games = preset.Games;

        // Remove games that were removed from the preset
        _session.Value.Games = _session.Value.Games.Where(g =>
            preset.Games.Any(ig => ig.GameConfig.Id == g.GameConfig.Id)).ToList();
        
        // Reset play time for all games
        foreach (var game in _session.Value.Games)
            game.TotalPlayTime = TimeSpan.Zero;
        
        var addedGames = preset.Games.Where(g =>
            _session.Value.Games.All(ig => ig.GameConfig.Id != g.GameConfig.Id)).ToList();
        
        if (addedGames.Any())
        {
            var processes = await GetProcesses();
            foreach (var game in addedGames)
            {
                var process = processes.TryGetValue(game.GameConfig.ExePath, out var p) ? p.FirstOrDefault() : null;
                var gameProcess = new GameProcess(game.GameConfig, _processMonitor, true);
                await gameProcess.ConnectToExistingProcess(process, _initCts.Token);

                var sessionGame = new SessionGame
                {
                    GameConfig = game.GameConfig,
                    Process = gameProcess
                };

                _session.Value.Games.Add(sessionGame);
            }
        }

        if (_session.Value.Preset?.GameSwitchMode == SwitchMode.Bagged && _session.Value.BaggedGames != null)
        {
            // Ensure bag only contains games that are in the session
            _session.Value.BaggedGames = new Queue<SessionGame>(
                _session.Value.BaggedGames.Where(g => _session.Value.Games.Contains(g))
            );

            // Add new games to bag
            foreach (var game in addedGames)
            {
                var list = _session.Value.BaggedGames.ToList();
                list.Add(_session.Value.Games.First(g => g.GameConfig.Id == game.GameConfig.Id));
                _session.Value.BaggedGames = new Queue<SessionGame>(list);
            }
        }

        // If the next game was removed, choose a new one. 
        if (_session.Value.NextGame != null && !_session.Value.Games.Contains(_session.Value.NextGame))
            RollNextGame();

        _session.Changed();
        return _session.Value;
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
                    Process = new GameProcess(game.GameConfig, _processMonitor, true),
                }).ToList()
            };

            var processes = await GetProcesses();
            foreach (var game in session.Games)
            {
                processes.TryGetValue(game.Process.Config.ExePath, out var process);
                await game.Process.ConnectToExistingProcess(process?.First(), _initCts.Token);
            }

            _session.Value = session;
            return session;
        }
        finally
        {
            _isLoading.Value = false;
        }
    }

    private void RollNextGame()
    {
        // TODO: 
        // var runningGames = _session.Value.Games.Where(g => g.Process.State.Value != GameState.Stopped).ToList();
        var runningGames = _session.Value.Games.Where(g => g.GameConfig.Id != _session.Value.CurrentGame?.GameConfig.Id)
            .ToList();
        if (!runningGames.Any()) return;

        var (nextGame, baggedGames) = RollNext(
            runningGames,
            _session.Value.Preset?.GameSwitchMode,
            _session.Value.CurrentGame,
            g => g.TotalPlayTime,
            _session.Value.BaggedGames);

        _session.Value.NextGame = nextGame;
        _session.Value.BaggedGames = baggedGames;
        _session.Changed();
    }

    private void RollNextPlayer()
    {
        var activePlayers = _session.Value.Players
            .Where(p => p.IsActive && p.Config.Name != _session.Value.CurrentPlayer?.Config.Name).ToList();
        if (!activePlayers.Any()) return;

        var (nextPlayer, baggedPlayers) = RollNext(
            activePlayers,
            _session.Value.Preset?.PlayerSwitchMode,
            _session.Value.CurrentPlayer,
            p => p.TotalPlayTime,
            _session.Value.BaggedPlayers);

        _session.Value.NextPlayer = nextPlayer;
        _session.Value.BaggedPlayers = baggedPlayers;
        _session.Changed();
    }

    private (T next, Queue<T>? baggedItems) RollNext<T>(
        List<T> items,
        SwitchMode? mode,
        T? current,
        Func<T, TimeSpan> getPlayTime,
        Queue<T>? baggedItems) where T : class
    {
        T? next = null;
        Queue<T>? updatedBaggedItems = baggedItems;

        switch (mode)
        {
            case SwitchMode.Random:
                next = items[Random.Shared.Next(items.Count())];
                break;
            case SwitchMode.Sequential:
                var currentIndex = current != null ? items.IndexOf(current) : -1;
                next = items[(currentIndex + 1) % items.Count()];
                break;
            case SwitchMode.LeastPlayed:
                next = items.OrderBy(getPlayTime).First();
                break;
            case SwitchMode.Bagged:
                if (updatedBaggedItems != null)
                    updatedBaggedItems = new Queue<T>(updatedBaggedItems.Where(items.Contains));

                if (updatedBaggedItems == null || !updatedBaggedItems.Any())
                    updatedBaggedItems = new Queue<T>(items.OrderBy(x => Random.Shared.Next()));

                next = updatedBaggedItems.Dequeue();
                break;
            default:
                next = items[Random.Shared.Next(items.Count())];
                break;
        }

        return (next, mode == SwitchMode.Bagged ? updatedBaggedItems : null);
    }

    private async Task<Dictionary<string, List<Process>>> GetProcesses()
    {
        // TODO: Just keep this up to date with our watches
        return await Task.Run(() =>
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

    public void Dispose()
    {
        _initCts.Cancel();
        _initCts.Dispose();
        _shuffleCts?.Cancel();
        _shuffleCts?.Dispose();
        _gamepadManager?.Dispose();
    }
}