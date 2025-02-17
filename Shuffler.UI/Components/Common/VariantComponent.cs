using Microsoft.AspNetCore.Components;
using Shuffler.UI.Utilities;
using System.ComponentModel;

namespace Shuffler.UI.Components.Common;

public class ClassAccessor<TSlot>(Variants<TSlot>? variants, IEnumerable<Enum> currentVariants)
    where TSlot : Enum
{
    public string this[TSlot slot] => variants?.GetClass(slot, currentVariants) ?? string.Empty;
}

public abstract class VariantComponent<TSlot> : ComponentBase
    where TSlot : Enum
{
    private Variants<TSlot>? _variants;
    private static Variants<TSlot>? _staticVariants;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Class { get; set; } = "";

    protected abstract Variants<TSlot> CreateVariants();
    protected abstract IEnumerable<Enum> GetCurrentVariants();

    protected override void OnParametersSet()
    {
#if DEBUG
        _variants = CreateVariants();
#else
        _staticVariants ??= CreateVariants();
        _variants = _staticVariants;
#endif
        base.OnParametersSet();
    }

    protected string BaseClass => _variants?.GetClass(Class, GetCurrentVariants()) ?? string.Empty;
    protected ClassAccessor<TSlot> SlotClass => new(_variants, GetCurrentVariants());
}