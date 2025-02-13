namespace Shuffler.Core.Models;

public class GameInfo
{
    public string Name { get; set; } = string.Empty;
    public string PortraitUrl { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;

    public string ImageUrl => !string.IsNullOrEmpty(PortraitUrl) ? PortraitUrl : IconUrl;
}