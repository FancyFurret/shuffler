using System.ComponentModel.DataAnnotations;

namespace Shuffler.Core.Models;

public class PresetGame : IValidatableObject
{
    public required GameConfig GameConfig { get; set; }

    [Range(5, int.MaxValue, ErrorMessage = "Minimum shuffle time must be at least 5 seconds")]
    [Required(ErrorMessage = "Minimum shuffle time is required when setting an override")]
    public int? MinShuffleTime { get; set; }

    [Range(5, int.MaxValue, ErrorMessage = "Maximum shuffle time must be at least 5 seconds")]
    [Required(ErrorMessage = "Maximum shuffle time is required when setting an override")]
    public int? MaxShuffleTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxShuffleTime.HasValue && MinShuffleTime.HasValue && MaxShuffleTime < MinShuffleTime)
        {
            yield return new ValidationResult(
                "Maximum shuffle time must be greater than or equal to minimum shuffle time",
                [nameof(MinShuffleTime), nameof(MaxShuffleTime)]
            );
        }
    }
}