using Microsoft.AspNetCore.Components;

namespace Shuffler.UI.Components.Common;

public abstract class DialogFormBase<TData> : DialogFormBase<TData, TData>
{
    protected override TData ToModel(TData data) => data;
    protected override TData ToData(TData model) => model;
}

public abstract class DialogFormBase<TData, TModel> : ComponentBase
{
    protected DialogForm<TModel>? DialogForm { get; set; }
    public TModel? Model => DialogForm is null ? default : DialogForm.Model;

    public async Task<TData?> Show(TData model, bool editing = false)
    {
        if (DialogForm is null) return default;
        var result = await DialogForm.Show(ToModel(model), editing);
        return result != null ? ToData(Model!) : default;
    }

    private async Task Hide(bool success)
    {
        if (DialogForm is null) return;
        await DialogForm.Hide(success);
    }
    
    protected abstract TModel ToModel(TData data);
    protected abstract TData ToData(TModel model);

    protected override void OnAfterRender(bool firstRender)
    {
        // DialogForm?.Refresh();
    }
}
