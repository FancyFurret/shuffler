using System.Collections.Concurrent;
using Shuffler.Core.Models;

namespace Shuffler.Core.Services;

public class GameInfoService
{
    private readonly ILogger _logger;
    private readonly SteamLibraryService _steamLibrary;
    private readonly SteamApiService _steamApi;
    private readonly ExecutableInfoService _exeInfo;
    private readonly ConcurrentDictionary<string, GameInfo> _gameInfoCache = new();

    public GameInfoService(ShufflerConfig config)
    {
        _logger = LogManager.Create(nameof(GameInfoService));
        _steamLibrary = new SteamLibraryService(config);
        _steamApi = new SteamApiService();
        _exeInfo = new ExecutableInfoService();
    }

    public GameInfo? GetGameInfo(GameConfig config)
    {
        var cacheKey = config.SteamAppId?.ToString() ?? config.ExePath;
        _gameInfoCache.TryGetValue(cacheKey, out var gameInfo);
        return gameInfo;
    }

    public async Task<GameInfo> GetGameInfoAsync(GameConfig config)
    {
        var cacheKey = config.SteamAppId?.ToString() ?? config.ExePath;
        if (_gameInfoCache.TryGetValue(cacheKey, out var cachedInfo))
        {
            return cachedInfo;
        }

        return await UpdateGameInfoAsync(config);
    }

    public async Task<GameInfo> UpdateGameInfoAsync(GameConfig config)
    {
        var cacheKey = config.SteamAppId?.ToString() ?? config.ExePath;
        var gameInfo = new GameInfo { Name = config.Name };

        try
        {
            // Get exe info if path exists
            if (!string.IsNullOrEmpty(config.ExePath))
            {
                var exeInfo = await _exeInfo.GetExecutableInfoAsync(config.ExePath);
                gameInfo.IconUrl = exeInfo.Base64Icon ?? string.Empty;

                // Try to detect Steam AppId if not provided
                config.SteamAppId ??= _steamLibrary.GetSteamAppIdFromExePath(config.ExePath);
            }

            // Get Steam info if available
            if (config.SteamAppId.HasValue)
            {
                var steamInfo = await _steamApi.GetSteamGameInfoAsync(config.SteamAppId.Value.ToString());
                gameInfo.Name = steamInfo.Name;
                gameInfo.PortraitUrl = steamInfo.PortraitUrl;
            }

            _gameInfoCache.AddOrUpdate(cacheKey, gameInfo, (_, _) => gameInfo);
            return gameInfo;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting game info for {config.Name}: {ex.Message}");
            return gameInfo;
        }
    }

    public async Task PreloadGamesAsync(IEnumerable<GameConfig> games)
    {
        await Task.WhenAll(games.Select(GetGameInfoAsync));
    }
}