using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shuffler.UI.Extensions;

public static class ElementReferenceExtensions
{
    public static async Task TryAddClass(this ElementReference elementRef, string className, IJSRuntime js)
    {
        try
        {
            await js.InvokeVoidAsync("eval", $"document.querySelector('[_bl_{elementRef.Id}]')?.classList.add('{className}')");
        }
        catch
        {
            // Ignore errors if element not found
        }
    }

    public static async Task TryRemoveClass(this ElementReference elementRef, string className, IJSRuntime js)
    {
        try
        {
            await js.InvokeVoidAsync("eval", $"document.querySelector('[_bl_{elementRef.Id}]')?.classList.remove('{className}')");
        }
        catch
        {
            // Ignore errors if element not found
        }
    }
}