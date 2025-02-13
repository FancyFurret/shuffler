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
    [Required(ErrorMessage = "Game name is required")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Game path is required")]
    public required string ExePath { get; set; }

    public int? SteamAppId { get; set; }
    public bool EnableHook { get; set; }
    public bool Suspend { get; set; }
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