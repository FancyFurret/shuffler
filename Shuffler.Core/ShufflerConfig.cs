using System.ComponentModel.DataAnnotations;
using Shuffler.Core.Models;

namespace Shuffler.Core;

public record ShufflerConfig
{
    [ValidSteamPath] public string? SteamPath { get; set; }
    public string SteamApiKey { get; set; } = string.Empty;
    public string DiscordToken { get; set; } = string.Empty;
    public int OverlayRefreshRate { get; set; } = 60;
    public List<GameConfig> Games { get; set; } = new();
    public List<PlayerConfig> Players { get; set; } = new();
    public List<ControllerLayout> ControllerLayouts { get; set; } = new();
    public List<ShufflerPreset> Presets { get; set; } = new();
    public string? LastUsedPresetId { get; set; }
    public List<string> LastUsedPlayerNames { get; set; } = new();
}

public record GameConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "Game name is required")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Game path is required")]
    public required string ExePath { get; set; }

    public bool EnableHook { get; set; }
    public bool Suspend { get; set; }

    public int? SteamAppId { get; set; }
}

public record PlayerConfig
{
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Color is required")]
    public string Color { get; set; } = string.Empty;

    [RegularExpression(@"^(?:https?:\/\/)?(?:www\.)?steamcommunity\.com\/(?:id\/[a-zA-Z0-9_-]+|profiles\/\d+)\/?$",
        ErrorMessage = "Please enter a valid Steam profile URL")]
    public string? SteamProfile { get; set; }

    public ulong? DiscordUserId { get; set; }
}

public class ValidSteamPathAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return ValidationResult.Success;

        var steamappsPath = Path.Combine(path, "steamapps");
        if (!Directory.Exists(steamappsPath))
            return new ValidationResult("The specified path must contain a 'steamapps' folder", [
                validationContext.MemberName!
            ]);

        return ValidationResult.Success;
    }
}