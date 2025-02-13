using System.Text;

namespace Shuffler.UI.Utilities;

public enum Size
{
    Small,
    Medium,
    Large
}

public class Variants<TSlot>
    where TSlot : Enum
{
    private static readonly string[] OverridableUtilities =
    {
        "justify-",
        "items-",
        "flex-",
        "grid-",
        "w-",
        "h-",
        "min-w-",
        "min-h-",
        "max-w-",
        "max-h-",
    };

    public string Base { get; init; } = string.Empty;
    private Dictionary<Enum, string> Styles { get; } = [];
    private Dictionary<(Enum Value, TSlot Slot), string> SlotStyles { get; } = [];
    private Dictionary<Enum[], string> CompoundStyles { get; } = [];

    public string this[TSlot slot]
    {
        set => Styles[slot] = value;
    }

    public string this[Enum variant]
    {
        set => Styles[variant] = value;
    }

    public string this[Enum variant, TSlot slot]
    {
        set => SlotStyles[(variant, slot)] = value;
    }

    private bool ShouldSkipUtility(string variantClass, string userClass)
    {
        return OverridableUtilities.Any(util =>
            variantClass.StartsWith(util, StringComparison.OrdinalIgnoreCase) &&
            userClass.StartsWith(util, StringComparison.OrdinalIgnoreCase));
    }

    private string FilterClasses(string variantClasses, string userClasses)
    {
        if (string.IsNullOrWhiteSpace(userClasses)) return variantClasses;

        var variantClassList = variantClasses.Split(' ');
        var userClassList = userClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Keep variant classes that aren't overridden by user classes
        var filteredVariantClasses = variantClassList
            .Where(vc => !userClassList.Any(uc => ShouldSkipUtility(vc, uc)))
            .ToList();

        // Combine both lists
        filteredVariantClasses.AddRange(userClassList);

        return string.Join(' ', filteredVariantClasses);
    }

    public string GetClass(IEnumerable<Enum?> variants)
    {
        return GetClass("", variants.ToArray());
    }

    public string GetClass(string userClass, IEnumerable<Enum?> variants)
    {
        return GetClass(userClass, variants.ToArray());
    }

    public string GetClass(params Enum?[] variants)
    {
        return GetClass("", variants);
    }

    public string GetClass(string userClass, params Enum?[] variants)
    {
        var classes = new StringBuilder();

        // Build variant classes
        var variantClasses = new StringBuilder(Base);
        foreach (var variant in variants)
            if (variant != null && Styles.TryGetValue(variant, out var style))
                variantClasses.Append(' ').Append(style);

        // Add compound styles
        var nonNullVariants = variants.Where(v => v != null).Cast<Enum>().ToArray();
        foreach (var compound in CompoundStyles)
        {
            if (compound.Key.All(k => nonNullVariants.Contains(k)) &&
                compound.Key.Length == compound.Key.Intersect(nonNullVariants).Count())
            {
                variantClasses.Append(' ').Append(compound.Value);
            }
        }

        // Filter out overridden utilities
        classes.Append(FilterClasses(variantClasses.ToString(), userClass ?? ""));

        return classes.ToString().Trim();
    }

    public string GetClass(TSlot slot, IEnumerable<Enum?> variants, string? userClass = null)
    {
        return GetClass(slot, variants.ToArray(), userClass);
    }

    public string GetClass(TSlot slot, Enum?[] variants, string? userClass = null)
    {
        var classes = new StringBuilder();

        if (Styles.TryGetValue(slot, out var baseStyle))
            classes.Append(baseStyle);

        foreach (var variant in variants)
            if (variant != null && SlotStyles.TryGetValue((variant, slot), out var style))
                classes.Append(' ').Append(style);

        return FilterClasses(classes.ToString(), userClass ?? "").Trim();
    }
}