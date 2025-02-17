using Shuffler.Core.Models;
using Shuffler.Core;

namespace Shuffler.UI.Services;

public class HomeStateService
{
    private readonly ShufflerConfig _config;
    private readonly ConfigurationService _configService;
    private ShufflerPreset? _savedPreset;
    private ShufflerCore _core;

    public HomeStateService(ShufflerConfig config, ConfigurationService configService, ShufflerCore core)
    {
        _config = config;
        _configService = configService;
        _core = core;
        LoadLastUsedPreset();
    }

    public ShufflerPreset CurrentPreset { get; private set; } = new();
    public string? CurrentPresetId { get; private set; }
    public bool HasChanges { get; private set; }
    public List<string> LastUsedPlayerNames => _config.LastUsedPlayerNames;

    private void LoadLastUsedPreset()
    {
        if (!string.IsNullOrEmpty(_config.LastUsedPresetId))
        {
            var preset = _config.Presets.FirstOrDefault(p => p.Id == _config.LastUsedPresetId);
            if (preset != null)
            {
                SetCurrentPreset(preset, preset.Id);
                return;
            }
        }

        Clear();
    }

    public void SetCurrentPreset(ShufflerPreset preset, string? presetId = null)
    {
        _savedPreset = new ShufflerPreset
        {
            Id = preset.Id,
            Name = preset.Name,
            MinShuffleTime = preset.MinShuffleTime,
            MaxShuffleTime = preset.MaxShuffleTime,
            Games = preset.Games.Select(g => new PresetGame
            {
                GameConfig = g.GameConfig,
                MinShuffleTime = g.MinShuffleTime,
                MaxShuffleTime = g.MaxShuffleTime
            }).ToList()
        };
        CurrentPreset = preset;
        CurrentPresetId = presetId;
        HasChanges = presetId == null;

        _config.LastUsedPresetId = presetId;
        SaveConfig();
    }

    public void UpdateLastUsedPlayers(IEnumerable<PlayerConfig> players)
    {
        _config.LastUsedPlayerNames = players.Select(p => p.Name).ToList();
        SaveConfig();
    }

    private void SaveConfig()
    {
        _configService.SaveAsync(_config).ConfigureAwait(false);
    }

    public void SetHasChanges(bool hasChanges = true)
    {
        HasChanges = hasChanges;
        _ = _core.UpdatePreset(CurrentPreset);
    }

    public void Clear()
    {
        _savedPreset = null;
        CurrentPreset = ShufflerPreset.Default();
        CurrentPresetId = null;
        _config.LastUsedPresetId = null;
        SaveConfig();
        HasChanges = false;
    }

    public void RevertChanges()
    {
        if (_savedPreset == null) return;

        CurrentPreset = new ShufflerPreset
        {
            Id = _savedPreset.Id,
            Name = _savedPreset.Name,
            MinShuffleTime = _savedPreset.MinShuffleTime,
            MaxShuffleTime = _savedPreset.MaxShuffleTime,
            Games = _savedPreset.Games.Select(g => new PresetGame
            {
                GameConfig = g.GameConfig,
                MinShuffleTime = g.MinShuffleTime,
                MaxShuffleTime = g.MaxShuffleTime
            }).ToList()
        };
        HasChanges = false;
    }
}