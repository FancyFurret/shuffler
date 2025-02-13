using System.Collections.Generic;

namespace Shuffler.Core.Models;

public class PlayerColorPreset
{
    public required string Name { get; init; }
    public required string Gradient { get; init; }
}

public static class PlayerColorPresets
{
    // Reds
    public static readonly PlayerColorPreset Crimson = new()
    {
        Name = "Crimson",
        Gradient = "linear-gradient(135deg, #991b1b, #f87171)"
    };

    public static readonly PlayerColorPreset Ember = new()
    {
        Name = "Ember",
        Gradient = "linear-gradient(135deg, #9a3412, #fb923c)"
    };

    // Orange/Yellow
    public static readonly PlayerColorPreset Sunrise = new()
    {
        Name = "Sunrise",
        Gradient = "linear-gradient(135deg, #c2410c, #fbbf24)"
    };

    public static readonly PlayerColorPreset Golden = new()
    {
        Name = "Golden",
        Gradient = "linear-gradient(135deg, #b45309, #fde047)"
    };

    // Greens
    public static readonly PlayerColorPreset Emerald = new()
    {
        Name = "Emerald",
        Gradient = "linear-gradient(135deg, #065f46, #4ade80)"
    };

    public static readonly PlayerColorPreset Forest = new()
    {
        Name = "Forest",
        Gradient = "linear-gradient(135deg, #166534, #a3e635)"
    };

    // Teals/Cyans
    public static readonly PlayerColorPreset Lagoon = new()
    {
        Name = "Lagoon",
        Gradient = "linear-gradient(135deg, #155e75, #22d3ee)"
    };

    // Blues
    public static readonly PlayerColorPreset Ocean = new()
    {
        Name = "Ocean",
        Gradient = "linear-gradient(135deg, #1e3a8a, #60a5fa)"
    };

    public static readonly PlayerColorPreset Arctic = new()
    {
        Name = "Arctic",
        Gradient = "linear-gradient(135deg, #1d4ed8, #bae6fd)"
    };

    // Purples
    public static readonly PlayerColorPreset Royal = new()
    {
        Name = "Royal",
        Gradient = "linear-gradient(135deg, #4c1d95, #a78bfa)"
    };

    public static readonly PlayerColorPreset Dusk = new()
    {
        Name = "Dusk",
        Gradient = "linear-gradient(135deg, #581c87, #d8b4fe)"
    };

    // Pinks
    public static readonly PlayerColorPreset Rose = new()
    {
        Name = "Rose",
        Gradient = "linear-gradient(135deg, #9d174d, #fda4af)"
    };

    // Special multi-color gradients
    public static readonly PlayerColorPreset Aurora = new()
    {
        Name = "Aurora",
        Gradient = "linear-gradient(135deg, #047857, #0ea5e9, #6366f1)"
    };

    public static readonly PlayerColorPreset Galaxy = new()
    {
        Name = "Galaxy",
        Gradient = "linear-gradient(135deg, #0f172a, #4338ca, #7e22ce, #fde047)"
    };

    public static readonly PlayerColorPreset Sunset = new()
    {
        Name = "Sunset",
        Gradient = "linear-gradient(135deg, #7c2d12, #c026d3, #fbbf24)"
    };

    public static readonly PlayerColorPreset Neon = new()
    {
        Name = "Neon",
        Gradient = "linear-gradient(135deg, #16a34a, #fbbf24, #e11d48)"
    };

    public static IEnumerable<PlayerColorPreset> All => new[]
    {
        Crimson, Ember, Sunrise, Golden,
        Emerald, Forest, Lagoon, Ocean,
        Arctic, Royal, Dusk, Rose,
        Aurora, Galaxy, Sunset, Neon
    };
}