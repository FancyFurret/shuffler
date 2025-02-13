using Microsoft.AspNetCore.Components;

namespace Shuffler.UI.Components.Common;

public class DialogPicker<TItem> : ComponentBase, IAsyncDisposable
{
    protected Dialog? Dialog { get; set; }
    private TaskCompletionSource<TItem?>? _tcs;

    protected void OnItemSelected(TItem item)
    {
        _tcs?.TrySetResult(item);
        _ = Hide();
    }

    public async Task<TItem?> Show()
    {
        _tcs = new TaskCompletionSource<TItem?>();
        if (Dialog is null) return default;

        await Dialog.Show();
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
        if (Dialog is null) return;
        await Dialog.Hide();
        _tcs?.TrySetCanceled();
    }

    public async ValueTask DisposeAsync()
    {
        await Hide();
    }
}