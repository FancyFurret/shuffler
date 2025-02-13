using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Shuffler.Core;
using ILogger = Shuffler.Core.ILogger;

namespace Shuffler.UI.Services;

public class AvatarService : IAsyncDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string _steamApiKey;
    private readonly ConcurrentDictionary<string, string> _avatarCache = new();
    private readonly DiscordSocketClient _discordClient;
    private readonly string _discordToken;
    private bool _isDiscordConnected;

    private readonly ILogger _logger = LogManager.Create(nameof(AvatarService));

    public AvatarService(ShufflerConfig config)
    {
        _steamApiKey = config.SteamApiKey;
        _discordToken = config.DiscordToken;

        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.GuildMembers, // We need this to fetch guild members
            LogLevel = LogSeverity.Error,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 0
        };
        _discordClient = new DiscordSocketClient(socketConfig);

        // Preload avatars for all players
        _ = PreloadPlayersAsync(config.Players);
    }

    public string? GetAvatarUrl(PlayerConfig player, bool large = false)
    {
        // Try Steam first if profile is set
        if (!string.IsNullOrEmpty(player.SteamProfile))
        {
            var cacheKey = $"steam:{player.SteamProfile}";
            if (_avatarCache.TryGetValue(cacheKey, out var avatarHash) && !string.IsNullOrEmpty(avatarHash))
            {
                var size = large ? "full" : "medium";
                return $"https://avatars.steamstatic.com/{avatarHash}_{size}.jpg";
            }
        }

        // Try Discord if user ID is set
        if (player.DiscordUserId.HasValue)
        {
            var cacheKey = $"discord:{player.DiscordUserId}";
            if (_avatarCache.TryGetValue(cacheKey, out var avatarHash))
            {
                var size = (ushort)(large ? 256 : 128);
                if (!string.IsNullOrEmpty(avatarHash))
                    return CDN.GetUserAvatarUrl(player.DiscordUserId.Value, avatarHash, size, ImageFormat.Auto);
            }
        }

        // Return default avatar if nothing is cached
        return null;
    }

    public async Task PreloadPlayersAsync(IEnumerable<PlayerConfig> players)
    {
        await EnsureDiscordConnectedAsync();

        var tasks = new List<Task>();
        foreach (var player in players)
        {
            if (!string.IsNullOrEmpty(player.SteamProfile))
                tasks.Add(UpdateSteamAvatarAsync(player));
            if (player.DiscordUserId.HasValue)
                tasks.Add(UpdateDiscordAvatarAsync(player));
        }

        await Task.WhenAll(tasks);
    }

    public async Task UpdatePlayerAvatarAsync(PlayerConfig player)
    {
        await EnsureDiscordConnectedAsync();

        var tasks = new List<Task>();

        if (!string.IsNullOrEmpty(player.SteamProfile))
            tasks.Add(UpdateSteamAvatarAsync(player));
        if (player.DiscordUserId.HasValue)
            tasks.Add(UpdateDiscordAvatarAsync(player));

        await Task.WhenAll(tasks);
    }

    private async Task EnsureDiscordConnectedAsync()
    {
        if (_isDiscordConnected) return;

        try
        {
            await _discordClient.LoginAsync(TokenType.Bot, _discordToken);
            await _discordClient.StartAsync();
            _isDiscordConnected = true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to connect to Discord");
        }
    }

    private async Task UpdateDiscordAvatarAsync(PlayerConfig player)
    {
        if (!player.DiscordUserId.HasValue) return;

        try
        {
            var user = await _discordClient.Rest.GetUserAsync(player.DiscordUserId.Value);
            if (user != null)
            {
                _avatarCache.TryAdd($"discord:{player.DiscordUserId}", user.AvatarId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating Discord avatar for player {player.Name}: {ex.Message}");
        }
    }

    private async Task UpdateSteamAvatarAsync(PlayerConfig player)
    {
        try
        {
            var steam64Id = await ResolveSteamIdAsync(player.SteamProfile!);
            if (string.IsNullOrEmpty(steam64Id)) return;

            var summaryUrl =
                $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_steamApiKey}&steamids={steam64Id}";
            var response = await _httpClient.GetStringAsync(summaryUrl);
            var data = JsonSerializer.Deserialize<JsonElement>(response);

            var players = data.GetProperty("response").GetProperty("players");
            if (players.GetArrayLength() > 0)
            {
                var steamPlayer = players[0];
                var avatarHash = steamPlayer.GetProperty("avatarhash").GetString();

                if (!string.IsNullOrEmpty(avatarHash))
                {
                    _avatarCache.TryAdd($"steam:{player.SteamProfile}", avatarHash);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating Steam avatar for player {player.Name}: {ex.Message}");
        }
    }

    private async Task<string> ResolveSteamIdAsync(string input)
    {
        try
        {
            // Try to match Steam64 ID pattern (17 digits)
            if (Regex.IsMatch(input, @"^[0-9]{17}$"))
                return input;

            // Try to match profile URL patterns
            var match = Regex.Match(input, @"steamcommunity\.com/id/([^/]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var vanityUrl = match.Groups[1].Value;
                var resolveUrl =
                    $"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={_steamApiKey}&vanityurl={vanityUrl}";
                var response = await _httpClient.GetStringAsync(resolveUrl);
                var data = JsonSerializer.Deserialize<JsonElement>(response);

                if (data.GetProperty("response").GetProperty("success").GetInt32() == 1)
                {
                    return data.GetProperty("response").GetProperty("steamid").GetString() ?? string.Empty;
                }
            }

            // Try to extract Steam64 ID from profile URL
            match = Regex.Match(input, @"steamcommunity\.com/profiles/([0-9]{17})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error resolving Steam ID for input '{input}': {ex.Message}");
            return string.Empty;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_discordClient != null)
        {
            await _discordClient.StopAsync();
            await _discordClient.DisposeAsync();
        }
    }
}