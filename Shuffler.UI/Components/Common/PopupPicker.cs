using Microsoft.AspNetCore.Components;

namespace Shuffler.UI.Components.Common;

public class PopupPicker<TItem> : ComponentBase, IAsyncDisposable
{
    protected Popup? Popup { get; set; }
    private TaskCompletionSource<TItem?>? _tcs;

    protected void OnItemSelected(TItem item)
    {
        _tcs?.TrySetResult(item);
        _ = Hide();
    }

    public async Task<TItem?> Show()
    {
        _tcs = new TaskCompletionSource<TItem?>();
        if (Popup is null) return default;

        Popup.Show();
        try
        {
            return await _tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return default;
        }
    }

    public async Task<TItem?> Show(int x, int y)
    {
        _tcs = new TaskCompletionSource<TItem?>();
        if (Popup is null) return default;

        Popup.Show(x, y);
        try
        {
            return await _tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return default;
        }
    }

    public async Task Hide()
    {
        if (Popup is null) return;
        await Popup.Hide();
        _tcs?.TrySetCanceled();
    }

    public async ValueTask DisposeAsync()
    {
        await Hide();
    }
}