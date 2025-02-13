using System.Text.Json;
using Shuffler.Core.Models;

namespace Shuffler.Core.Services;

public class SteamApiService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient = new();

    public SteamApiService()
    {
        _logger = LogManager.Create(nameof(SteamApiService));
    }

    public async Task<GameInfo> GetSteamGameInfoAsync(string appId)
    {
        try
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}";
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<JsonElement>(response);

            if (data.TryGetProperty(appId, out var appData) &&
                appData.GetProperty("success").GetBoolean())
            {
                var gameData = appData.GetProperty("data");
                var gameInfo = new GameInfo
                {
                    Name = gameData.GetProperty("name").GetString() ?? string.Empty,
                };

                // Try vertical image
                gameInfo.PortraitUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900.jpg";
                try
                {
                    var portraitResponse = await _httpClient.GetAsync(gameInfo.PortraitUrl);
                    if (!portraitResponse.IsSuccessStatusCode)
                    {
                        // Try header image as fallback
                        gameInfo.PortraitUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg";
                        var headerResponse = await _httpClient.GetAsync(gameInfo.PortraitUrl);
                        if (!headerResponse.IsSuccessStatusCode)
                        {
                            gameInfo.PortraitUrl = string.Empty;
                        }
                    }
                }
                catch
                {
                    gameInfo.PortraitUrl = string.Empty;
                }

                return gameInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching Steam game info for appId {appId}: {ex.Message}");
        }

        return new GameInfo { Name = $"Unknown Game ({appId})" };
    }
}