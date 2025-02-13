using System.ComponentModel.DataAnnotations;

namespace Shuffler.Core;

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