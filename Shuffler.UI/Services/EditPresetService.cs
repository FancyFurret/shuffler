using Shuffler.Core.Models;
using Shuffler.Core;

namespace Shuffler.UI.Services;

public class EditPresetService
{
    private readonly ShufflerConfig _config;
    private readonly ConfigurationService _configService;
    private readonly ShufflerCore _core;
    
    private ShufflerPreset? _savedPreset;

    public EditPresetService(ShufflerConfig config, ConfigurationService configService, ShufflerCore core)
    {
        _config = config;
        _configService = configService;
        _core = core;
        LoadLastUsedPreset();
    }

    public ShufflerPreset CurrentPreset { get; private set; } = new();
    public bool HasChanges { get; private set; }

    private void LoadLastUsedPreset()
    {
        if (!string.IsNullOrEmpty(_config.LastUsedPresetId))
        {
            var preset = _config.Presets.FirstOrDefault(p => p.Id == _config.LastUsedPresetId);
            if (preset != null)
            {
                _ = LoadPreset(preset);
                return;
            }
        }

        _savedPreset = null;
        CurrentPreset = ShufflerPreset.Default();
        _config.LastUsedPresetId = null;
        SaveConfig();
        HasChanges = false;
    }

    public async Task LoadPreset(ShufflerPreset preset)
    {
        _savedPreset = preset.Duplicate();
        CurrentPreset = preset.Duplicate();
        HasChanges = preset.Id == null;
        _config.LastUsedPresetId = preset.Id;
        SaveConfig();
        
        await _core.LoadPreset(preset);
    }

    private void SaveConfig()
    {
        _configService.SaveAsync(_config).ConfigureAwait(false);
    }

    public void MarkChanged()
    {
        HasChanges = true;
        _ = _core.UpdatePreset(CurrentPreset);
    }

    public void RevertChanges()
    {
        if (_savedPreset == null) return;
        CurrentPreset = _savedPreset.Duplicate();
        HasChanges = false;
        _ = _core.UpdatePreset(CurrentPreset);
    }
}