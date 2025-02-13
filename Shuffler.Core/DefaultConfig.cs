using Shuffler.Core.Models;

namespace Shuffler.Core;

public static class DefaultConfig
{
    public static ShufflerConfig Create()
    {
        return new ShufflerConfig
        {
            OverlayRefreshRate = 60,
            Games = [],
            Players =
            [
                new PlayerConfig
                {
                    Name = "Player 1",
                    Color = PlayerColorPresets.Ocean.Gradient
                }
            ],
            Presets = []
        };
    }
}