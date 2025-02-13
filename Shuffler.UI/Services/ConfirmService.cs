using Microsoft.AspNetCore.Components;

namespace Shuffler.UI.Services;

public class ConfirmService
{
    private readonly NavigationManager _navigationManager;
    private event Func<string, string?, bool, bool, Task<bool>>? OnConfirmRequested;

    public ConfirmService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        // _navigationManager.LocationChanged += (s, e) => OnConfirmRequested = null;
    }

    public void RegisterDialog(Func<string, string?, bool, bool, Task<bool>> callback)
    {
        OnConfirmRequested = callback;
    }

    public Task<bool> Confirm(string message, string? title = null, bool isDestructive = false)
    {
        if (OnConfirmRequested == null)
            throw new InvalidOperationException("No confirmation dialog is registered. Make sure to add <ConfirmDialog /> to your App.razor or MainLayout.razor");

        return OnConfirmRequested.Invoke(message, title, isDestructive, false);
    }

    public Task Alert(string message, string? title = null)
    {
        if (OnConfirmRequested == null)
            throw new InvalidOperationException("No confirmation dialog is registered. Make sure to add <ConfirmDialog /> to your App.razor or MainLayout.razor");

        return OnConfirmRequested.Invoke(message, title, false, true);
    }
}