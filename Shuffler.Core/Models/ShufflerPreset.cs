using System.ComponentModel.DataAnnotations;

namespace Shuffler.Core.Models;

public class ShufflerPreset : IValidatableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required] public string Name { get; set; } = "";
    public List<PresetGame> Games { get; set; } = [];

    [Range(5, int.MaxValue, ErrorMessage = "Minimum shuffle time must be at least 5 seconds")]
    public int MinShuffleTime { get; set; }

    [Range(5, int.MaxValue, ErrorMessage = "Maximum shuffle time must be at least 5 seconds")]
    public int MaxShuffleTime { get; set; }

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
}