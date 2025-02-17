using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shuffler.Core.Models;

public class ShufflerPreset : IValidatableObject
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Preset name is required")]
    [MinLength(1, ErrorMessage = "Preset name cannot be empty")]
    public string Name { get; set; } = "Unsaved Preset";

    [Range(3, int.MaxValue, ErrorMessage = "Min shuffle time must be at least 3 seconds")]
    public int MinShuffleTime { get; set; } = 30;

    [Range(3, int.MaxValue, ErrorMessage = "Max shuffle time must be at least 3 seconds")]
    public int MaxShuffleTime { get; set; } = 60;

    public SwitchMode GameSwitchMode { get; set; } = SwitchMode.Random;
    public SwitchMode PlayerSwitchMode { get; set; } = SwitchMode.Random;

    public bool ShowNextUpcoming { get; set; } = true;

    [Required(ErrorMessage = "At least one game is required")]
    [MinLength(1, ErrorMessage = "At least one game is required")]
    public List<PresetGame> Games { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxShuffleTime < MinShuffleTime)
        {
            yield return new ValidationResult(
                "Maximum shuffle time must be greater than or equal to minimum shuffle time",
                [nameof(MinShuffleTime), nameof(MaxShuffleTime)]
            );
        }

        if (Games.Count == 0)
        {
            yield return new ValidationResult(
                "At least one game must be added to the preset",
                [nameof(Games)]
            );
        }
    }
    
    public static ShufflerPreset Default()
    {
        return new ShufflerPreset
        {
            Name = "Unsaved Preset",
            Games = [],
            MinShuffleTime = 30,
            MaxShuffleTime = 60,
            GameSwitchMode = SwitchMode.Bagged,
            PlayerSwitchMode = SwitchMode.LeastPlayed,
            ShowNextUpcoming = true
        };
    }
}

public enum SwitchMode
{
    Random,
    Sequential,
    LeastPlayed,
    Bagged
}

public static class SwitchModeExtensions
{
    public static string GetDisplayName(this SwitchMode mode) =>
        mode switch
        {
            SwitchMode.Random => "Random",
            SwitchMode.Sequential => "Sequential",
            SwitchMode.LeastPlayed => "Least Played",
            SwitchMode.Bagged => "Bagged",
            _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(SwitchMode))
        };

    public static string GetDescription(this SwitchMode mode) =>
        mode switch
        {
            SwitchMode.Random => "Randomly selects with equal probability",
            SwitchMode.Sequential => "Cycles through in order",
            SwitchMode.LeastPlayed => "Prioritizes those that have been played the least",
            SwitchMode.Bagged => "Uses a \"bag\" system where each is guaranteed to be picked once before any repeats",
            _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(SwitchMode))
        };

    public static string GetIcon(this SwitchMode mode) =>
        mode switch
        {
            SwitchMode.Random => "arrows-right-left",
            SwitchMode.Sequential => "arrow-path",
            SwitchMode.LeastPlayed => "chart-bar",
            SwitchMode.Bagged => "squares-2x2",
            _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(SwitchMode))
        };
}